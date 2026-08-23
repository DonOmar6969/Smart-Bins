using SmartBins.Datos;
using SmartBins.Modelos;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SmartBins.Vistas
{
    public partial class InventarioVistaPagina : Page
    {
        private readonly BinDatos binDatos = new BinDatos();
        private readonly ComponentesDatos componentesDatos = new ComponentesDatos();
        private Bin binSeleccionado = null;
        private Componente componenteSeleccionado = null;
        private string rutaFotoComponente = null;
        private string NumeroSeleccionado = "";
        private List<Bin> bins = new();
        private List<Componente> componentes = new();
        private readonly List<int> componentesPendientesIds = new();
        private readonly bool flujoActualizacionEnsamble;

        public InventarioVistaPagina()
            : this(Array.Empty<int>())
        {
        }

        public InventarioVistaPagina(IEnumerable<int> componentesPendientes)
        {
            InitializeComponent();
            CargarNivelesTiempoConfigurados();
            componentesPendientesIds.AddRange(componentesPendientes.Distinct());
            flujoActualizacionEnsamble = componentesPendientesIds.Any();
            CargarBins();
            CargarComponentes();
            CargarTiposComponente();
            MostrarComponentesPendientes();
        }

        private void CargarNivelesTiempoConfigurados()
        {
            CmbNivelTiempo.Items.Clear();
            var niveles = Servicios.ConfiguracionAplicacionService.Cargar().NivelesTiempoCicloSegundos;
            for (int i = 0; i < niveles.Count; i++)
            {
                CmbNivelTiempo.Items.Add(new ComboBoxItem
                {
                    Content = $"Nivel {i + 1} — {niveles[i]} seg",
                    Tag = niveles[i].ToString()
                });
            }
        }

        private static List<string> ObtenerCamposFaltantes(Componente componente)
        {
            var campos = new List<string>();
            if (string.IsNullOrWhiteSpace(componente.FotoRuta) ||
                !File.Exists(componente.FotoRuta))
                campos.Add("fotografía");
            if (componente.TiempoCiclo <= 0)
                campos.Add("tiempo de ciclo");
            return campos;
        }

        private void MostrarComponentesPendientes()
        {
            if (!flujoActualizacionEnsamble) return;

            InventarioTabs.SelectedIndex = 1;
            PanelGuiaActualizacion.Visibility = Visibility.Visible;

            var pendientes = componentes
                .Where(c => componentesPendientesIds.Contains(c.ComponenteID) &&
                            ObtenerCamposFaltantes(c).Any())
                .ToList();

            componentesPendientesIds.Clear();
            componentesPendientesIds.AddRange(pendientes.Select(c => c.ComponenteID));

            if (!pendientes.Any())
            {
                TxtGuiaActualizacion.Text =
                    "✓ Todos los componentes están actualizados. Regresando a Nuevo Ensamble…";
                PanelGuiaActualizacion.Background =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(236, 253, 243));
                LimpiarResaltadosActualizacion();
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    "Todos los componentes importados quedaron completamente actualizados.\n\n" +
                    "Se regresará a Nuevo Ensamble con el avance conservado.",
                    "Actualización completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (NavigationService?.CanGoBack == true)
                    NavigationService.GoBack();
                else
                    NavigationService?.Navigate(new NuevoEnsambleVistaPagina());
                return;
            }

            GridComponentes.ItemsSource = pendientes;
            GridComponentes.SelectedItem = pendientes.First();
            GridComponentes.ScrollIntoView(pendientes.First());
            ActualizarGuiaComponenteSeleccionado();
        }

        private void ActualizarGuiaComponenteSeleccionado()
        {
            if (!componentesPendientesIds.Any() || componenteSeleccionado == null) return;

            LimpiarResaltadosActualizacion();
            var campos = ObtenerCamposFaltantes(componenteSeleccionado);

            if (campos.Contains("fotografía"))
            {
                ResaltarActualizacion(BtnSeleccionarFotoComponente);
            }
            if (campos.Contains("tiempo de ciclo"))
            {
                ResaltarActualizacion(CmbNivelTiempo);
            }
            ResaltarActualizacion(BtnActualizarComponente);

            int posicion = componentesPendientesIds.IndexOf(componenteSeleccionado.ComponenteID) + 1;
            TxtGuiaActualizacion.Text =
                $"Componente pendiente {posicion} de {componentesPendientesIds.Count}: " +
                $"{componenteSeleccionado.NumeroParte} — completa " +
                $"{string.Join(" y ", campos)} y presiona “Actualizar”.";
        }

        private static void ResaltarActualizacion(UIElement elemento)
        {
            elemento.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Color.FromRgb(247, 144, 9),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.95
            };
        }

        private void LimpiarResaltadosActualizacion()
        {
            BtnSeleccionarFotoComponente.Effect = null;
            CmbNivelTiempo.Effect = null;
            BtnActualizarComponente.Effect = null;
        }

        // ===================== BINS =====================

        private void CargarBins()
        {
            bins = binDatos.ObtenerTodos();
            AplicarFiltroBins();
            ActualizarContadores();
        }

        private void AgregarBin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtBinCodigo.Text) ||
                string.IsNullOrWhiteSpace(TxtBinDescripcion.Text))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Por favor completa todos los campos.");
                return;
            }

            Bin nuevo = new Bin
            {
                Codigo = TxtBinCodigo.Text.Trim(),
                Descripcion = TxtBinDescripcion.Text.Trim()
            };

            binDatos.Agregar(nuevo);
            CargarBins();
            LimpiarBin();
        }

        private void ActualizarBin_Click(object sender, RoutedEventArgs e)
        {
            if (binSeleccionado == null)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona un bin de la lista primero.");
                return;
            }

            binSeleccionado.Descripcion = TxtBinDescripcion.Text.Trim();
            binDatos.Actualizar(binSeleccionado);
            CargarBins();
            LimpiarBin();
        }

        private void EliminarBin_Click(object sender, RoutedEventArgs e)
        {
            if (binSeleccionado == null)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona un bin de la lista primero.");
                return;
            }

            MessageBoxResult resultado = global::SmartBins.Servicios.DialogoSmart.Mostrar(
                $"¿Eliminar el bin {binSeleccionado.Codigo}?",
                "Confirmar", MessageBoxButton.YesNo);

            if (resultado == MessageBoxResult.Yes)
            {
                binDatos.Eliminar(binSeleccionado.Codigo);
                CargarBins();
                LimpiarBin();
            }
        }

        private void LimpiarBin_Click(object sender, RoutedEventArgs e) => LimpiarBin();

        private void LimpiarBin()
        {
            TxtBinCodigo.Text = string.Empty;
            TxtBinDescripcion.Text = string.Empty;
            binSeleccionado = null;
            GridBins.SelectedItem = null;
        }

        private void GridBins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            binSeleccionado = GridBins.SelectedItem as Bin;
            if (binSeleccionado != null)
            {
                TxtBinCodigo.Text = binSeleccionado.Codigo;
                TxtBinDescripcion.Text = binSeleccionado.Descripcion;
            }
        }

        // ===================== COMPONENTES =====================

        private void CargarComponentes()
        {
            componentes = componentesDatos.ObtenerTodos();
            AplicarFiltroComponentes();
            ActualizarContadores();
        }

        private void TxtBusquedaGlobal_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroBins();
            AplicarFiltroComponentes();
        }

        private void ActualizarResumen_Click(object sender, RoutedEventArgs e)
        {
            CargarBins();
            CargarComponentes();
            CargarTiposComponente();
        }

        private void AplicarFiltroBins()
        {
            if (GridBins == null) return;

            string busqueda = TxtBusquedaGlobal?.Text.Trim() ?? string.Empty;
            GridBins.ItemsSource = string.IsNullOrWhiteSpace(busqueda)
                ? bins
                : bins.Where(b =>
                    b.Codigo?.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase) == true ||
                    b.Descripcion?.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase) == true)
                    .ToList();
        }

        private void AplicarFiltroComponentes()
        {
            if (GridComponentes == null) return;

            string busqueda = TxtBusquedaGlobal?.Text.Trim() ?? string.Empty;
            GridComponentes.ItemsSource = string.IsNullOrWhiteSpace(busqueda)
                ? componentes
                : componentes.Where(c =>
                    c.Nombre.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase) ||
                    c.NumeroParte.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase) ||
                    c.Descripcion.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
        }

        private void ActualizarContadores()
        {
            if (TxtTotalBins == null ||
                TxtTotalComponentes == null ||
                TxtComponentesPendientes == null)
                return;

            TxtTotalBins.Text = bins.Count.ToString();
            TxtTotalComponentes.Text = componentes.Count.ToString();
            TxtComponentesPendientes.Text = componentes.Count(c =>
                string.Equals(c.Estatus, "Desactualizado", StringComparison.CurrentCultureIgnoreCase))
                .ToString();
        }

        private void CargarTiposComponente()
        {
            CmbNombreComponente.ItemsSource = componentesDatos.ObtenerTipos();
        }

        private void CmbNombreComponente_GotFocus(object sender, RoutedEventArgs e)
        {
            CargarTiposComponente();
        }

        // ── NIVEL TIEMPO CICLO ──────────────────────────────────────────
        private void CmbNivelTiempo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbNivelTiempo.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Tag?.ToString(), out int segundos))
            {
                TxtTiempoCicloSeleccionado.Text = $"{segundos} seg";
            }
        }

        private void PreseleccionarNivel(decimal tiempoCiclo)
        {
            int seg = (int)tiempoCiclo;
            foreach (ComboBoxItem item in CmbNivelTiempo.Items)
            {
                if (item.Tag?.ToString() == seg.ToString())
                {
                    CmbNivelTiempo.SelectedItem = item;
                    TxtTiempoCicloSeleccionado.Text = $"{seg} seg";
                    return;
                }
            }
            // Si no coincide con ningún nivel (ej. 0), limpiar
            CmbNivelTiempo.SelectedItem = null;
            TxtTiempoCicloSeleccionado.Text = "— seg";
        }

        private decimal ObtenerTiempoCicloSeleccionado()
        {
            if (CmbNivelTiempo.SelectedItem is ComboBoxItem item &&
                decimal.TryParse(item.Tag?.ToString(), out decimal seg))
                return seg;
            return 0;
        }

        private string CalcularEstatus(string fotoRuta, decimal tiempoCiclo)
        {
            bool tieneImagen = !string.IsNullOrWhiteSpace(fotoRuta) && File.Exists(fotoRuta);
            bool tieneTiempo = tiempoCiclo > 0;
            return (tieneImagen && tieneTiempo) ? "Actualizado" : "Desactualizado";
        }

        // ── FOTO ────────────────────────────────────────────────────────
        private void SeleccionarFotoComponente_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CmbNombreComponente.Text))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Primero ingresa el nombre del componente.");
                return;
            }

            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.avif;*.webp";
            dialog.Title = "Selecciona la foto del componente";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string carpetaDestino = Path.Combine(escritorio, "Smart_Bins_Imagenes", "Componentes");
                    Directory.CreateDirectory(carpetaDestino);

                    string extension = Path.GetExtension(dialog.FileName);
                    string nombreArchivo = $"{CmbNombreComponente.Text.Trim()}_{NumeroSeleccionado.Trim()}{extension}";
                    string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);

                    File.Copy(dialog.FileName, rutaDestino, true);
                    rutaFotoComponente = rutaDestino;
                    TxtRutaFotoComponente.Text = nombreArchivo;
                    MostrarImagen(rutaDestino);
                }
                catch (Exception ex)
                {
                    global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error al copiar la imagen: {ex.Message}");
                }
            }
        }

        private void MostrarImagen(string ruta)
        {
            if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
            {
                BitmapImage imagen = new BitmapImage();
                imagen.BeginInit();
                imagen.UriSource = new Uri(ruta);
                imagen.CacheOption = BitmapCacheOption.OnLoad;
                imagen.EndInit();
                ImgComponente.Source = imagen;
                TxtSinImagen.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImgComponente.Source = null;
                TxtSinImagen.Visibility = Visibility.Visible;
            }
        }

        // ── CRUD COMPONENTES ────────────────────────────────────────────
        private void AgregarComponente_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CmbNombreComponente.Text))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("El nombre del componente es obligatorio.");
                return;
            }

            string numeroParte = TxtComponenteNumeroParte.Text.Trim();
            if (string.IsNullOrWhiteSpace(numeroParte))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    "El número de parte es obligatorio para identificar el componente.",
                    "Número de parte requerido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtComponenteNumeroParte.Focus();
                return;
            }

            if (componentesDatos.ExisteNumeroParte(numeroParte))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    $"Ya existe un componente con el número de parte “{numeroParte}”.\n\n" +
                    "Selecciona el componente existente si deseas actualizarlo.",
                    "Componente duplicado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtComponenteNumeroParte.Focus();
                return;
            }

            decimal tiempoCiclo = ObtenerTiempoCicloSeleccionado();

            Componente nuevo = new Componente
            {
                Nombre = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CmbNombreComponente.Text.Trim().ToLower()),
                NumeroParte = numeroParte,
                Descripcion = TxtComponenteDescripcion.Text.Trim(),
                FotoRuta = rutaFotoComponente,
                TiempoCiclo = tiempoCiclo,
                Estatus = CalcularEstatus(rutaFotoComponente, tiempoCiclo)
            };

            if (!componentesDatos.Agregar(nuevo))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    $"No se agregó el componente porque el número de parte “{numeroParte}” ya existe.",
                    "Componente duplicado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            CargarComponentes();
            CargarTiposComponente();
            LimpiarComponente();
        }

        private void ActualizarComponente_Click(object sender, RoutedEventArgs e)
        {
            if (componenteSeleccionado == null)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona un componente de la lista primero.");
                return;
            }

            string numeroParte = TxtComponenteNumeroParte.Text.Trim();
            if (string.IsNullOrWhiteSpace(numeroParte))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    "El número de parte es obligatorio para identificar el componente.",
                    "Número de parte requerido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtComponenteNumeroParte.Focus();
                return;
            }

            if (componentesDatos.ExisteNumeroParte(
                    numeroParte, componenteSeleccionado.ComponenteID))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    $"Otro componente ya utiliza el número de parte “{numeroParte}”.",
                    "Componente duplicado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtComponenteNumeroParte.Focus();
                return;
            }

            decimal tiempoCiclo = ObtenerTiempoCicloSeleccionado();

            componenteSeleccionado.Nombre = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CmbNombreComponente.Text.Trim().ToLower());
            componenteSeleccionado.NumeroParte = numeroParte;
            componenteSeleccionado.Descripcion = TxtComponenteDescripcion.Text.Trim();
            componenteSeleccionado.TiempoCiclo = tiempoCiclo;

            if (rutaFotoComponente != null)
                componenteSeleccionado.FotoRuta = rutaFotoComponente;

            componenteSeleccionado.Estatus = CalcularEstatus(componenteSeleccionado.FotoRuta, tiempoCiclo);

            componentesDatos.Actualizar(componenteSeleccionado);
            CargarComponentes();
            CargarTiposComponente();
            LimpiarComponente();
            MostrarComponentesPendientes();
        }

        private void EliminarComponente_Click(object sender, RoutedEventArgs e)
        {
            if (componenteSeleccionado == null)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona un componente de la lista primero.");
                return;
            }

            MessageBoxResult resultado = global::SmartBins.Servicios.DialogoSmart.Mostrar(
                $"¿Eliminar el componente {componenteSeleccionado.Nombre}?",
                "Confirmar", MessageBoxButton.YesNo);

            if (resultado == MessageBoxResult.Yes)
            {
                string fotoEliminada = componenteSeleccionado.FotoRuta;
                componentesDatos.Eliminar(componenteSeleccionado.ComponenteID);
                int imagenesEliminadas =
                    ImagenesArchivosService.EliminarFotoDeComponente(fotoEliminada);
                CargarComponentes();
                CargarTiposComponente();
                LimpiarComponente();
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    $"Componente eliminado correctamente. Se eliminaron " +
                    $"{imagenesEliminadas} imagen(es) asociadas.");
            }
        }

        private void LimpiarComponente_Click(object sender, RoutedEventArgs e) => LimpiarComponente();

        private void LimpiarComponente()
        {
            CmbNombreComponente.Text = string.Empty;
            TxtComponenteNumeroParte.Text = string.Empty;
            TxtComponenteDescripcion.Text = string.Empty;
            TxtRutaFotoComponente.Text = "Sin imagen";
            CmbNivelTiempo.SelectedItem = null;
            TxtTiempoCicloSeleccionado.Text = "— seg";
            rutaFotoComponente = null;
            componenteSeleccionado = null;
            GridComponentes.SelectedItem = null;
            MostrarImagen(null);
        }

        private void GridComponentes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            componenteSeleccionado = GridComponentes.SelectedItem as Componente;
            if (componenteSeleccionado != null)
            {
                CmbNombreComponente.Text = componenteSeleccionado.Nombre;
                TxtComponenteNumeroParte.Text = componenteSeleccionado.NumeroParte ?? string.Empty;
                NumeroSeleccionado = componenteSeleccionado.NumeroParte ?? string.Empty;
                TxtComponenteDescripcion.Text = componenteSeleccionado.Descripcion;
                rutaFotoComponente = null;
                TxtRutaFotoComponente.Text = "Sin imagen";
                MostrarImagen(componenteSeleccionado.FotoRuta);
                PreseleccionarNivel(componenteSeleccionado.TiempoCiclo);
                ActualizarGuiaComponenteSeleccionado();
            }
        }
    }
}

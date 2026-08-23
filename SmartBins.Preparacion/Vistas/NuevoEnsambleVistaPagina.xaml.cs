using SmartBins.Datos;
using SmartBins.Modelos;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SmartBins.Vistas
{
    // ==========================================
    // CONVERTIDOR PARA CARGAR IMÁGENES SIN BLOQUEAR EL ARCHIVO
    // ==========================================
    public class RutaAImagenSinBloqueoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string ruta = value as string;
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                return null;
            try
            {
                BitmapImage imagen = new BitmapImage();
                imagen.BeginInit();
                imagen.UriSource = new Uri(ruta, UriKind.Absolute);
                imagen.CacheOption = BitmapCacheOption.OnLoad;
                imagen.EndInit();
                return imagen;
            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public partial class NuevoEnsambleVistaPagina : Page
    {
        private readonly ComponentesDatos componentesDatos = new ComponentesDatos();
        private readonly EnsambleDatos ensambleDatos = new EnsambleDatos();
        private readonly SecuenciaEnsambleDatos secuenciaDatos = new SecuenciaEnsambleDatos();
        private ObservableCollection<SecuenciaEnsamble> secuencia = new ObservableCollection<SecuenciaEnsamble>();
        private ObservableCollection<string> rutasImagenesPasoActual = new ObservableCollection<string>();
        private readonly HashSet<string> imagenesCreadasEnSesion =
            new(StringComparer.OrdinalIgnoreCase);
        private string rutaExcelSeleccionado = "";
        private static BorradorEnsamble borradorPendiente;
        private static readonly string rutaBorrador = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBins", "borrador-nuevo-ensamble.json");

        private sealed class BorradorEnsamble
        {
            public BorradorEnsamble() { }

            public string Nombre { get; set; } = string.Empty;
            public string UnidadTrabajo { get; set; } = string.Empty;
            public int Operadores { get; set; } = 1;
            public List<SecuenciaEnsamble> Secuencia { get; set; } = new();
            public int ComponenteSeleccionadoId { get; set; }
            public string Tiempo { get; set; } = string.Empty;
            public string Estacion { get; set; } = string.Empty;
            public string Referencia { get; set; } = string.Empty;
            public string OrdenPrecedente { get; set; } = string.Empty;
            public List<string> ImagenesPasoActual { get; set; } = new();
            public List<string> ImagenesCreadasEnSesion { get; set; } = new();
        }

        public NuevoEnsambleVistaPagina()
        {
            InitializeComponent();
            Servicios.EntradaNumericaService.RestringirADecimal(TxtTiempo);
            Servicios.EntradaNumericaService.RestringirAEnteros(TxtEstacion, TxtOrdenPrecedente);
            TxtOperadoresDefault.Text = Servicios.ConfiguracionAplicacionService.Cargar()
                .OperadoresPredeterminados.ToString();
            CargarTipos();
            GridSecuencia.ItemsSource = secuencia;
            GaleriaImagenes.ItemsSource = rutasImagenesPasoActual;
            RestaurarBorradorSiExiste();
            ActualizarGuiaFlujo();
        }

        public NuevoEnsambleVistaPagina(Ensamble ensamble, List<SecuenciaEnsamble> secuenciaExistente)
        {
            InitializeComponent();
            Servicios.EntradaNumericaService.RestringirADecimal(TxtTiempo);
            Servicios.EntradaNumericaService.RestringirAEnteros(TxtEstacion, TxtOrdenPrecedente);
            CargarTipos();
            GaleriaImagenes.ItemsSource = rutasImagenesPasoActual;

            TxtNombreEnsamble.Text = ensamble.Nombre;
            TxtNombreEnsamble.IsEnabled = false;
            TxtOperadoresDefault.Text = ensamble.NumeroOperadoresDefault.ToString();
            CmbUnidadTrabajo.Text = ensamble.UnidadTrabajo;

            foreach (var item in secuenciaExistente)
                secuencia.Add(item);

            GridSecuencia.ItemsSource = secuencia;
            ActualizarGuiaFlujo();
        }

        // ==========================================
        // NIVELES DE TIEMPO CICLO
        // ==========================================


        

        // ==========================================
        // GALERÍA DE IMÁGENES
        // ==========================================
        private void SeleccionarImagenPaso_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNombreEnsamble.Text))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Primero ingresa el nombre del ensamble.");
                return;
            }

            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp";
            dialog.Title = "Selecciona una imagen para el paso";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string carpetaDestino = Path.Combine(escritorio, "Smart_Bins_Imagenes", "Secuencia");
                    Directory.CreateDirectory(carpetaDestino);

                    string extension = Path.GetExtension(dialog.FileName);
                    int orden = secuencia.Count + 1;
                    if (GridSecuencia.SelectedItem is SecuenciaEnsamble pasoSeleccionado)
                        orden = pasoSeleccionado.OrdenSecuencia;

                    string nombreArchivo = $"{TxtNombreEnsamble.Text.Trim()}_{orden}_{Guid.NewGuid().ToString().Substring(0, 5)}{extension}";
                    string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);

                    File.Copy(dialog.FileName, rutaDestino, true);
                    imagenesCreadasEnSesion.Add(rutaDestino);
                    rutasImagenesPasoActual.Add(rutaDestino);
                    ActualizarVisibilidadGaleria();
                    ActualizarImagenesEnPasoSeleccionado();
                }
                catch (Exception ex)
                {
                    global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error al copiar la imagen: {ex.Message}");
                }
            }
        }

        private void EliminarImagenPaso_Click(object sender, RoutedEventArgs e)
        {
            if (GaleriaImagenes.SelectedItem == null)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona una foto de la galería para eliminarla.");
                return;
            }

            string imagenSeleccionada = GaleriaImagenes.SelectedItem as string;
            rutasImagenesPasoActual.Remove(imagenSeleccionada);
            ActualizarVisibilidadGaleria();
            ActualizarImagenesEnPasoSeleccionado();

            try
            {
                if (!string.IsNullOrWhiteSpace(imagenSeleccionada))
                {
                    ImagenesArchivosService.EliminarImagenesNuevasDeBorrador(
                        new[] { imagenSeleccionada });
                    imagenesCreadasEnSesion.Remove(imagenSeleccionada);
                }
            }
            catch (Exception ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"La imagen se removió del paso, pero el archivo físico no pudo eliminarse: {ex.Message}",
                                "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ActualizarVisibilidadGaleria()
        {
            TxtSinImagenPaso.Visibility = rutasImagenesPasoActual.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            GaleriaImagenes.Visibility = rutasImagenesPasoActual.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ActualizarImagenesEnPasoSeleccionado()
        {
            if (GridSecuencia.SelectedItem is SecuenciaEnsamble paso)
            {
                paso.ImagenRuta = string.Join(";", rutasImagenesPasoActual);
                GridSecuencia.Items.Refresh();
            }
        }

        private void GridSecuencia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            rutasImagenesPasoActual.Clear();
            if (GridSecuencia.SelectedItem is SecuenciaEnsamble item && !string.IsNullOrWhiteSpace(item.ImagenRuta))
            {
                foreach (string r in item.ImagenRuta.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    if (File.Exists(r)) rutasImagenesPasoActual.Add(r);
            }
            ActualizarVisibilidadGaleria();
        }

        // ==========================================
        // GESTIÓN DE COMPONENTES Y SECUENCIA
        // ==========================================
        private void ListaComponentes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaComponentes.SelectedItem is Componente c)
            {
                // Pre-rellenar tiempo con el del inventario
                TxtTiempo.Text = c.TiempoCiclo > 0 ? c.TiempoCiclo.ToString() : string.Empty;

            }
            ActualizarGuiaFlujo();
        }

        private void AgregarComponenteSecuencia_Click(object sender, RoutedEventArgs e)
        {
            if (ListaComponentes.SelectedItem == null)
            {
                ActualizarGuiaFlujo();
                ListaComponentes.Focus();
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona un componente de la lista.");
                return;
            }

            if (!decimal.TryParse(TxtTiempo.Text, out decimal tiempo) || tiempo <= 0 || tiempo > 30)
            {
                ActualizarGuiaFlujo();
                BtnEditarTiempo.Focus();
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Ingresa un tiempo entre 0.01 y 30 segundos.");
                return;
            }

            if (!int.TryParse(TxtEstacion.Text, out int estacion) || estacion <= 0)
            {
                ActualizarGuiaFlujo();
                TxtEstacion.Focus();
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Ingresa un número de estación válido.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtReferencia.Text))
            {
                ActualizarGuiaFlujo();
                TxtReferencia.Focus();
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Ingresa la referencia del paso.");
                return;
            }

            int ordenPrecedente = 0;
            if (!string.IsNullOrWhiteSpace(TxtOrdenPrecedente.Text))
            {
                if (!int.TryParse(TxtOrdenPrecedente.Text, out ordenPrecedente))
                {
                    global::SmartBins.Servicios.DialogoSmart.Mostrar("El campo 'Antes de #' debe ser un número.");
                    return;
                }
            }

            Componente seleccionado = ListaComponentes.SelectedItem as Componente;
            string rutasConcatenadas = string.Join(";", rutasImagenesPasoActual);

            secuencia.Add(new SecuenciaEnsamble
            {
                OrdenSecuencia = secuencia.Count + 1,
                ComponenteID = seleccionado.ComponenteID,
                NumeroParte = seleccionado.NumeroParte,
                NombreComponente = seleccionado.Nombre,
                EspecificacionComponente = seleccionado.Descripcion,
                TiempoEstimado = tiempo,
                EstacionAsignada = estacion,
                Referencia = TxtReferencia.Text.Trim(),
                OrdenPrecedente = ordenPrecedente,
                ImagenRuta = rutasConcatenadas
            });

            // Limpiar campos pero mantener el componente seleccionado
            TxtTiempo.Text = string.Empty;
            TxtTiempo.IsEnabled = false;
            TxtEstacion.Text = string.Empty;
            TxtReferencia.Text = string.Empty;
            TxtOrdenPrecedente.Text = string.Empty;
            rutasImagenesPasoActual.Clear();
            ActualizarVisibilidadGaleria();
            ActualizarGuiaFlujo();
        }

        private void QuitarComponenteSecuencia_Click(object sender, RoutedEventArgs e)
        {
            if (GridSecuencia.SelectedItem == null)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona un componente de la secuencia.");
                return;
            }
            secuencia.Remove(GridSecuencia.SelectedItem as SecuenciaEnsamble);
            RecalcularOrden();
            ActualizarGuiaFlujo();
        }

        private void SubirComponente_Click(object sender, RoutedEventArgs e)
        {
            int index = GridSecuencia.SelectedIndex;
            if (index <= 0) return;
            secuencia.Move(index, index - 1);
            RecalcularOrden();
        }

        private void BajarComponente_Click(object sender, RoutedEventArgs e)
        {
            int index = GridSecuencia.SelectedIndex;
            if (index < 0 || index >= secuencia.Count - 1) return;
            secuencia.Move(index, index + 1);
            RecalcularOrden();
        }

        private void RecalcularOrden()
        {
            for (int i = 0; i < secuencia.Count; i++)
                secuencia[i].OrdenSecuencia = i + 1;
        }

        private void DisminuirOperadores_Click(object sender, RoutedEventArgs e)
        {
            int operadores = ObtenerCantidadOperadores();
            if (operadores > 1)
                TxtOperadoresDefault.Text = (operadores - 1).ToString();
            ActualizarGuiaFlujo();
        }

        private void AumentarOperadores_Click(object sender, RoutedEventArgs e)
        {
            TxtOperadoresDefault.Text = (ObtenerCantidadOperadores() + 1).ToString();
            ActualizarGuiaFlujo();
        }

        private int ObtenerCantidadOperadores()
        {
            return int.TryParse(TxtOperadoresDefault.Text, out int operadores) && operadores > 0
                ? operadores
                : 1;
        }

        private void GuardarBorrador()
        {
            borradorPendiente = new BorradorEnsamble
            {
                Nombre = TxtNombreEnsamble.Text,
                UnidadTrabajo = (CmbUnidadTrabajo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                                ?? CmbUnidadTrabajo.Text,
                Operadores = ObtenerCantidadOperadores(),
                Secuencia = secuencia.Select(ClonarPaso).ToList(),
                ComponenteSeleccionadoId =
                    (ListaComponentes.SelectedItem as Componente)?.ComponenteID ?? 0,
                Tiempo = TxtTiempo.Text,
                Estacion = TxtEstacion.Text,
                Referencia = TxtReferencia.Text,
                OrdenPrecedente = TxtOrdenPrecedente.Text,
                ImagenesPasoActual = rutasImagenesPasoActual.ToList(),
                ImagenesCreadasEnSesion = imagenesCreadasEnSesion.ToList()
            };

            string carpeta = Path.GetDirectoryName(rutaBorrador)!;
            Directory.CreateDirectory(carpeta);
            File.WriteAllText(rutaBorrador,
                JsonSerializer.Serialize(borradorPendiente,
                    new JsonSerializerOptions { WriteIndented = true }));
        }

        private void RestaurarBorradorSiExiste()
        {
            if (borradorPendiente == null && File.Exists(rutaBorrador))
            {
                try
                {
                    borradorPendiente = JsonSerializer.Deserialize<BorradorEnsamble>(
                        File.ReadAllText(rutaBorrador));
                }
                catch
                {
                    borradorPendiente = null;
                }
            }

            if (borradorPendiente == null) return;

            MessageBoxResult recuperar = global::SmartBins.Servicios.DialogoSmart.Mostrar(
                "Hay un avance de ensamble guardado.\n\n" +
                "¿Deseas recuperarlo?\n\n" +
                "Selecciona “No” para comenzar desde cero y eliminar el borrador.",
                "Borrador disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (recuperar == MessageBoxResult.No)
            {
                ImagenesArchivosService.EliminarImagenesNuevasDeBorrador(
                    borradorPendiente.ImagenesCreadasEnSesion);
                borradorPendiente = null;
                if (File.Exists(rutaBorrador))
                    File.Delete(rutaBorrador);
                return;
            }

            TxtNombreEnsamble.Text = borradorPendiente.Nombre;
            CmbUnidadTrabajo.Text = borradorPendiente.UnidadTrabajo;
            TxtOperadoresDefault.Text = borradorPendiente.Operadores.ToString();

            secuencia.Clear();
            foreach (SecuenciaEnsamble paso in borradorPendiente.Secuencia)
                secuencia.Add(ClonarPaso(paso));

            TxtTiempo.Text = borradorPendiente.Tiempo;
            TxtEstacion.Text = borradorPendiente.Estacion;
            TxtReferencia.Text = borradorPendiente.Referencia;
            TxtOrdenPrecedente.Text = borradorPendiente.OrdenPrecedente;

            rutasImagenesPasoActual.Clear();
            foreach (string ruta in borradorPendiente.ImagenesPasoActual)
                rutasImagenesPasoActual.Add(ruta);
            imagenesCreadasEnSesion.Clear();
            foreach (string ruta in borradorPendiente.ImagenesCreadasEnSesion)
                imagenesCreadasEnSesion.Add(ruta);
            ActualizarVisibilidadGaleria();

            if (borradorPendiente.ComponenteSeleccionadoId > 0)
            {
                ListaComponentes.SelectedItem = ListaComponentes.Items
                    .OfType<Componente>()
                    .FirstOrDefault(c => c.ComponenteID == borradorPendiente.ComponenteSeleccionadoId);
            }

            global::SmartBins.Servicios.DialogoSmart.Mostrar(
                "Se recuperó el avance del ensamble.",
                "Borrador recuperado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static SecuenciaEnsamble ClonarPaso(SecuenciaEnsamble paso) => new SecuenciaEnsamble
        {
            SecuenciaID = paso.SecuenciaID,
            EnsambleNombre = paso.EnsambleNombre,
            NumeroParte = paso.NumeroParte,
            NombreComponente = paso.NombreComponente,
            EspecificacionComponente = paso.EspecificacionComponente,
            TiempoEstimado = paso.TiempoEstimado,
            ComponenteID = paso.ComponenteID,
            OrdenSecuencia = paso.OrdenSecuencia,
            EstacionAsignada = paso.EstacionAsignada,
            Referencia = paso.Referencia,
            OrdenPrecedente = paso.OrdenPrecedente,
            ImagenRuta = paso.ImagenRuta
        };

        private void NuevoEnsambleVistaPagina_Loaded(object sender, RoutedEventArgs e)
        {
            SincronizarSecuenciaConInventario();
        }

        private void SincronizarSecuenciaConInventario()
        {
            if (secuencia.Count == 0) return;

            var componentesPorId = componentesDatos.ObtenerTodos()
                .ToDictionary(c => c.ComponenteID);
            bool huboCambios = false;

            foreach (SecuenciaEnsamble paso in secuencia)
            {
                if (!componentesPorId.TryGetValue(paso.ComponenteID, out Componente componente))
                    continue;

                if (paso.NumeroParte != componente.NumeroParte)
                {
                    paso.NumeroParte = componente.NumeroParte;
                    huboCambios = true;
                }

                if (paso.NombreComponente != componente.Nombre)
                {
                    paso.NombreComponente = componente.Nombre;
                    huboCambios = true;
                }

                if (paso.EspecificacionComponente != componente.Descripcion)
                {
                    paso.EspecificacionComponente = componente.Descripcion;
                    huboCambios = true;
                }

                // Los pasos importados sin tiempo toman el tiempo actualizado del inventario.
                // Un tiempo personalizado ya válido se conserva.
                if (paso.TiempoEstimado <= 0 && componente.TiempoCiclo > 0)
                {
                    paso.TiempoEstimado = componente.TiempoCiclo;
                    huboCambios = true;
                }
            }

            if (!huboCambios) return;

            GridSecuencia.Items.Refresh();
            ActualizarGuiaFlujo();

            if (borradorPendiente != null || File.Exists(rutaBorrador))
                GuardarBorrador();
        }

        private void CampoFlujo_TextChanged(object sender, TextChangedEventArgs e)
            => ActualizarGuiaFlujo();

        private void CampoFlujo_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ActualizarGuiaFlujo();

        private void ActualizarGuiaFlujo()
        {
            if (TxtGuiaFlujo == null || ListaComponentes == null) return;

            LimpiarResaltadosFlujo();

            if (string.IsNullOrWhiteSpace(TxtNombreEnsamble.Text))
            {
                MostrarSiguientePaso("Paso 1 de 7: escribe el nombre del ensamble.", TxtNombreEnsamble);
                return;
            }

            if (CmbUnidadTrabajo.SelectedIndex <= 0)
            {
                MostrarSiguientePaso("Paso 2 de 7: selecciona la unidad de trabajo.", CmbUnidadTrabajo);
                return;
            }

            if (secuencia.Count == 0 && ListaComponentes.SelectedItem == null)
            {
                MostrarSiguientePaso(
                    "Paso 3 de 7: ajusta los operadores y selecciona un componente, o usa “Importar Excel”.",
                    SelectorOperadores, ListaComponentes, BtnImportarExcel);
                return;
            }

            if (secuencia.Count == 0)
            {
                if (!decimal.TryParse(TxtTiempo.Text, out decimal tiempo) || tiempo <= 0 || tiempo > 30)
                {
                    MostrarSiguientePaso(
                        "Paso 4 de 7: ingresa un tiempo entre 0.01 y 30 segundos.",
                        TxtTiempo, BtnEditarTiempo);
                    return;
                }

                if (!int.TryParse(TxtEstacion.Text, out int estacion) || estacion <= 0)
                {
                    MostrarSiguientePaso("Paso 5 de 7: indica la estación del paso.", TxtEstacion);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtReferencia.Text))
                {
                    MostrarSiguientePaso("Paso 6 de 8: escribe la referencia del paso.", TxtReferencia);
                    return;
                }

                MostrarSiguientePaso("Paso 7 de 8: presiona “Agregar” para incluir el paso.", BtnAgregarPaso);
                return;
            }

            var pasosSinTiempo = secuencia.Where(s => s.TiempoEstimado <= 0)
                                         .Select(s => s.OrdenSecuencia).ToList();
            if (pasosSinTiempo.Any())
            {
                MostrarSiguientePaso(
                    $"Revisión necesaria: falta tiempo en los pasos {string.Join(", ", pasosSinTiempo)}. " +
                    "Esto puede ocurrir después de importar Excel.",
                    GridSecuencia);
                return;
            }

            var pasosSinEstacion = secuencia.Where(s => s.EstacionAsignada <= 0)
                                           .Select(s => s.OrdenSecuencia).ToList();
            if (pasosSinEstacion.Any())
            {
                MostrarSiguientePaso(
                    $"Revisión necesaria: falta estación en los pasos {string.Join(", ", pasosSinEstacion)}.",
                    GridSecuencia);
                return;
            }

            var pasosSinReferencia = secuencia.Where(s => string.IsNullOrWhiteSpace(s.Referencia))
                                              .Select(s => s.OrdenSecuencia).ToList();
            if (pasosSinReferencia.Any())
            {
                MostrarSiguientePaso(
                    $"Revisión necesaria: falta referencia en los pasos {string.Join(", ", pasosSinReferencia)}.",
                    GridSecuencia, TxtReferencia);
                return;
            }

            var pasosSinImagen = secuencia.Where(s => !TieneImagenProcedimiento(s))
                                         .Select(s => s.OrdenSecuencia).ToList();
            if (pasosSinImagen.Any())
            {
                MostrarSiguientePaso(
                    $"Revisión necesaria: agrega una imagen del procedimiento en los pasos " +
                    $"{string.Join(", ", pasosSinImagen)}. Selecciona el paso y usa “Agregar imagen”.",
                    GridSecuencia, GaleriaImagenes);
                return;
            }

            int operadores = ObtenerCantidadOperadores();
            var estacionesUsadas = secuencia.Select(s => s.EstacionAsignada).Distinct().ToList();
            var estacionesFaltantes = Enumerable.Range(1, operadores).Except(estacionesUsadas).ToList();
            if (estacionesFaltantes.Any())
            {
                MostrarSiguientePaso(
                    $"Faltan componentes para las estaciones {string.Join(", ", estacionesFaltantes)}.",
                    TxtEstacion, GridSecuencia);
                return;
            }

            MostrarSiguientePaso(
                "Paso 8 de 8: la secuencia está completa. Puedes agregar otro paso o guardar.",
                BtnAgregarPaso, BtnGuardarEnsamble);
        }

        private void MostrarSiguientePaso(string mensaje, params UIElement[] elementos)
        {
            TxtGuiaFlujo.Text = mensaje;
            foreach (UIElement elemento in elementos)
            {
                elemento.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromRgb(255, 166, 0),
                    BlurRadius = 16,
                    ShadowDepth = 0,
                    Opacity = 0.9
                };
            }
        }

        private void LimpiarResaltadosFlujo()
        {
            UIElement[] elementos =
            {
                TxtNombreEnsamble, CmbUnidadTrabajo, SelectorOperadores,
                ListaComponentes, TxtTiempo, TxtEstacion, TxtReferencia, BtnEditarTiempo,
                BtnAgregarPaso, BtnImportarExcel, GridSecuencia, GaleriaImagenes,
                BtnGuardarEnsamble
            };
            foreach (UIElement elemento in elementos)
                elemento.Effect = null;
        }

        private static bool TieneImagenProcedimiento(SecuenciaEnsamble paso)
        {
            if (string.IsNullOrWhiteSpace(paso.ImagenRuta)) return false;
            return paso.ImagenRuta.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                  .Any(ruta => File.Exists(ruta));
        }

        private bool RedirigirSiHayComponentesDesactualizados()
        {
            if (secuencia.Count == 0) return false;

            var componentesPorId = componentesDatos.ObtenerTodos()
                .ToDictionary(c => c.ComponenteID);
            var componentesPendientes = secuencia
                .Select(s => componentesPorId.TryGetValue(s.ComponenteID, out Componente componente)
                    ? componente
                    : new Componente
                    {
                        ComponenteID = s.ComponenteID,
                        Nombre = s.NombreComponente,
                        Estatus = "Desactualizado"
                    })
                .Where(c => c.TiempoCiclo <= 0 ||
                            string.IsNullOrWhiteSpace(c.FotoRuta) ||
                            !File.Exists(c.FotoRuta))
                .GroupBy(c => c.ComponenteID)
                .Select(g => g.First())
                .ToList();

            if (!componentesPendientes.Any()) return false;

            GuardarBorrador();
            global::SmartBins.Servicios.DialogoSmart.Mostrar(
                $"La importación contiene {componentesPendientes.Count} componente(s) " +
                "sin foto o sin tiempo de ciclo." +
                "\n\nEl avance quedó guardado como borrador. Se abrirá Inventario para completarlos.",
                "Componentes pendientes",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            NavigationService?.Navigate(new InventarioVistaPagina(
                componentesPendientes.Select(c => c.ComponenteID)));
            return true;
        }

        // ==========================================
        // GUARDAR ENSAMBLE
        // ==========================================
        private void GuardarEnsamble_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNombreEnsamble.Text))
            {
                ActualizarGuiaFlujo();
                TxtNombreEnsamble.Focus();
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Ingresa el nombre del ensamble.");
                return;
            }

            if (CmbUnidadTrabajo.SelectedIndex <= 0)
            {
                ActualizarGuiaFlujo();
                CmbUnidadTrabajo.Focus();
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Selecciona la unidad de trabajo.");
                return;
            }

            if (!int.TryParse(TxtOperadoresDefault.Text, out int operadoresDefault) || operadoresDefault <= 0)
            { global::SmartBins.Servicios.DialogoSmart.Mostrar("Ingresa un número de operadores válido."); return; }

            if (secuencia.Count == 0)
            {
                ActualizarGuiaFlujo();
                ListaComponentes.Focus();
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Agrega al menos un componente a la secuencia.");
                return;
            }

            var pasosIncompletos = secuencia
                .Where(s => s.TiempoEstimado <= 0 ||
                            s.EstacionAsignada <= 0 ||
                            string.IsNullOrWhiteSpace(s.Referencia))
                .ToList();
            if (pasosIncompletos.Any())
            {
                GridSecuencia.SelectedItem = pasosIncompletos.First();
                GridSecuencia.ScrollIntoView(pasosIncompletos.First());
                ActualizarGuiaFlujo();
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    $"Revisa los pasos incompletos: {string.Join(", ", pasosIncompletos.Select(s => s.OrdenSecuencia))}. " +
                    "Todos deben tener tiempo, estación y referencia.");
                return;
            }

            var componentesPorId = componentesDatos.ObtenerTodos()
                .ToDictionary(c => c.ComponenteID);
            var componentesNoActualizados = secuencia
                .Select(s => componentesPorId.TryGetValue(s.ComponenteID, out Componente componente)
                    ? componente
                    : new Componente
                    {
                        ComponenteID = s.ComponenteID,
                        Nombre = s.NombreComponente,
                        Estatus = "Desactualizado"
                    })
                .Where(c => c.TiempoCiclo <= 0 ||
                            string.IsNullOrWhiteSpace(c.FotoRuta) ||
                            !File.Exists(c.FotoRuta))
                .GroupBy(c => c.ComponenteID)
                .Select(g => g.First())
                .ToList();

            if (componentesNoActualizados.Any())
            {
                GuardarBorrador();
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    $"No se puede guardar el ensamble: {componentesNoActualizados.Count} componente(s) " +
                    "necesitan foto o tiempo de ciclo." +
                    "\n\nEl avance se guardó como borrador. Se abrirá Inventario para actualizarlos.",
                    "Componentes desactualizados",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                NavigationService?.Navigate(new InventarioVistaPagina(
                    componentesNoActualizados.Select(c => c.ComponenteID)));
                return;
            }

            var pasosSinImagenProcedimiento = secuencia
                .Where(s => !TieneImagenProcedimiento(s))
                .ToList();
            if (pasosSinImagenProcedimiento.Any())
            {
                GridSecuencia.SelectedItem = pasosSinImagenProcedimiento.First();
                GridSecuencia.ScrollIntoView(pasosSinImagenProcedimiento.First());
                ActualizarGuiaFlujo();
                global::SmartBins.Servicios.DialogoSmart.Mostrar(
                    "No se puede guardar el ensamble. Falta una imagen del procedimiento en los pasos: " +
                    string.Join(", ", pasosSinImagenProcedimiento.Select(s => s.OrdenSecuencia)) +
                    ".\n\nSelecciona cada paso y usa el botón “Agregar” de la sección Imágenes del paso.",
                    "Imágenes de procedimiento faltantes",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var estacionesUsadas = secuencia.Select(s => s.EstacionAsignada).Distinct().OrderBy(x => x).ToList();
            var estacionesEsperadas = Enumerable.Range(1, operadoresDefault).ToList();

            var fueraDeRango = estacionesUsadas.Except(estacionesEsperadas).ToList();
            if (fueraDeRango.Any())
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Estaciones inválidas: {string.Join(", ", fueraDeRango)}. Máximo permitido: {operadoresDefault}.");
                return;
            }

            var faltantes = estacionesEsperadas.Except(estacionesUsadas).ToList();
            if (faltantes.Any())
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Faltan componentes en estaciones: {string.Join(", ", faltantes)}.");
                return;
            }

            Ensamble ensamble = new Ensamble
            {
                Nombre = TxtNombreEnsamble.Text.Trim(),
                TiempoEstimado = (int)secuencia.Sum(s => s.TiempoEstimado),
                UnidadTrabajo = (CmbUnidadTrabajo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                NumeroOperadoresDefault = operadoresDefault
            };

            if (TxtNombreEnsamble.IsEnabled && ensambleDatos.Existe(ensamble.Nombre))
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Ya existe un ensamble con el nombre '{ensamble.Nombre}'.");
                return;
            }

            try
            {
                secuenciaDatos.EliminarPorEnsamble(ensamble.Nombre);

                if (TxtNombreEnsamble.IsEnabled)
                    ensambleDatos.Agregar(ensamble);
                else
                    ensambleDatos.Actualizar(ensamble);

                var lista = secuencia.Select(item => new SecuenciaEnsamble
                {
                    EnsambleNombre = ensamble.Nombre,
                    ComponenteID = item.ComponenteID,
                    NumeroParte = item.NumeroParte,
                    OrdenSecuencia = item.OrdenSecuencia,
                    TiempoEstimado = item.TiempoEstimado,
                    NombreComponente = item.NombreComponente,
                    EspecificacionComponente = item.EspecificacionComponente,
                    EstacionAsignada = item.EstacionAsignada,
                    Referencia = item.Referencia,
                    OrdenPrecedente = item.OrdenPrecedente,
                    ImagenRuta = item.ImagenRuta
                }).ToList();

                secuenciaDatos.AgregarLista(lista);
                global::SmartBins.Servicios.ControlEnsambleService.RegistrarGuardado(
                    ensamble.Nombre, TxtNombreEnsamble.IsEnabled);
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Ensamble guardado correctamente.");
                borradorPendiente = null;
                imagenesCreadasEnSesion.Clear();
                if (File.Exists(rutaBorrador))
                    File.Delete(rutaBorrador);

                secuencia.Clear();
                TxtNombreEnsamble.Text = string.Empty;
                TxtNombreEnsamble.IsEnabled = true;
                TxtOperadoresDefault.Text = Servicios.ConfiguracionAplicacionService.Cargar()
                    .OperadoresPredeterminados.ToString();
                CmbUnidadTrabajo.SelectedIndex = 0;
                rutasImagenesPasoActual.Clear();
                ActualizarVisibilidadGaleria();
                ActualizarGuiaFlujo();
            }
            catch (Exception ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error al guardar el ensamble: {ex.Message}");
            }
        }

        // ==========================================
        // FILTROS
        // ==========================================
        private void CargarTipos()
        {
            var tipos = componentesDatos.ObtenerTipos();
            tipos.Insert(0, "Todos");
            CmbTipo.ItemsSource = tipos;
            CmbTipo.SelectedIndex = 0;
        }

        private void CargarComponentesFiltrados()
        {
            string tipo = CmbTipo.SelectedItem?.ToString() == "Todos" ? null : CmbTipo.SelectedItem?.ToString();
            string parte = TxtFiltroEspecificacion.Text.Trim();
            ListaComponentes.ItemsSource = componentesDatos.ObtenerPorFiltro(tipo, parte);
        }

        private void CmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => CargarComponentesFiltrados();

        private void TxtFiltroEspecificacion_TextChanged(object sender, TextChangedEventArgs e)
            => CargarComponentesFiltrados();

        // ==========================================
        // IMPORTAR EXCEL
        // ==========================================
        private void SeleccionarExcel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Archivos de Excel (*.xlsx;*.xls)|*.xlsx;*.xls";
            dialog.Title = "Selecciona el archivo de secuencia";

            if (dialog.ShowDialog() != true) return;

            try
            {
                rutaExcelSeleccionado = dialog.FileName;

                var servicio = new ExcelImportadorService();
                var resultado = servicio.ImportarDesdeExcel(rutaExcelSeleccionado);

                if (!resultado.Filas.Any())
                {
                    string msg = resultado.Advertencias.Any()
                        ? string.Join("\n", resultado.Advertencias)
                        : "No se encontraron filas de primer ensamble (L.1) en el archivo.";
                    global::SmartBins.Servicios.DialogoSmart.Mostrar(msg, "Sin datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var ventanaPreview = new ImportarExcelVentana(resultado) { Owner = Window.GetWindow(this) };
                if (ventanaPreview.ShowDialog() == true)
                    ProcesarImportacion(ventanaPreview.FilasConfirmadas);
            }
            catch (Exception ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error al procesar el archivo:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcesarImportacion(List<FilaImportada> filas)
        {
            var todosComponentes = componentesDatos.ObtenerTodos();
            int agregados = 0;
            var componentesNuevos = new List<Componente>();

            foreach (var fila in filas)
            {
                var existente = todosComponentes.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.NumeroParte) &&
                    c.NumeroParte.Trim().Equals(fila.NumeroParte?.Trim(), StringComparison.OrdinalIgnoreCase));

                if (existente == null)
                {
                    var nuevo = new Componente
                    {
                        NumeroParte = fila.NumeroParte ?? string.Empty,
                        Nombre = fila.Nombre,
                        Descripcion = fila.Descripcion,
                        FotoRuta = string.Empty,
                        TiempoCiclo = 0,
                        Estatus = "Desactualizado"
                    };
                    componentesDatos.Agregar(nuevo);

                    existente = componentesDatos.ObtenerPorNumeroParte(fila.NumeroParte);
                    if (existente != null)
                    {
                        todosComponentes.Add(existente);
                        componentesNuevos.Add(existente);
                    }
                }

                if (existente == null) continue;

                secuencia.Add(new SecuenciaEnsamble
                {
                    OrdenSecuencia = secuencia.Count + 1,
                    ComponenteID = existente.ComponenteID,
                    NumeroParte = existente.NumeroParte,
                    NombreComponente = existente.Nombre,
                    EspecificacionComponente = existente.Descripcion,
                    TiempoEstimado = existente.TiempoCiclo > 0 ? existente.TiempoCiclo : 0,
                    EstacionAsignada = fila.EstacionAsignada > 0 ? fila.EstacionAsignada : 1,
                    Referencia = fila.Referencia,
                    OrdenPrecedente = 0,
                    ImagenRuta = string.Empty
                });
                agregados++;
            }

            if (componentesNuevos.Any())
                MostrarAlertaNoInvasiva(componentesNuevos);

            global::SmartBins.Servicios.DialogoSmart.Mostrar(
                $"Se cargaron {agregados} pasos a la secuencia." +
                (componentesNuevos.Any()
                    ? $"\n\n{componentesNuevos.Count} componente(s) nuevos agregados al inventario — revisa la alerta."
                    : string.Empty),
                "Importación completada", MessageBoxButton.OK, MessageBoxImage.Information);
            ActualizarGuiaFlujo();
            RedirigirSiHayComponentesDesactualizados();
        }

        private void MostrarAlertaNoInvasiva(List<Componente> componentesNuevos)
        {
            string nombres = string.Join(", ", componentesNuevos.Take(5).Select(c => c.NumeroParte));
            if (componentesNuevos.Count > 5) nombres += $" y {componentesNuevos.Count - 5} más";

            TxtBannerAlerta.Text = $"⚠  {componentesNuevos.Count} componente(s) sin imagen ni tiempo de ciclo: {nombres}. " +
                                   "Ve a Inventario → Componentes para completar su información.";
            BannerAlerta.Visibility = Visibility.Visible;
        }

        private void CerrarBanner_Click(object sender, RoutedEventArgs e)
            => BannerAlerta.Visibility = Visibility.Collapsed;

        private void BtnEditarTiempo_Click(object sender, RoutedEventArgs e)
        {
            TxtTiempo.IsEnabled = true;
            TxtTiempo.Focus();
            TxtTiempo.SelectAll();
            BtnEditarTiempo.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0066CC"));
        }

        private void TxtTiempo_LostFocus(object sender, RoutedEventArgs e)
        {
            TxtTiempo.IsEnabled = false;
            BtnEditarTiempo.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888888"));
        }
    }
}

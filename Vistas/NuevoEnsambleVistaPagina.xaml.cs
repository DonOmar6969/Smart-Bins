using SmartBins.Datos;
using SmartBins.Modelos;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SmartBins.Vistas
{
    public partial class NuevoEnsambleVistaPagina : Page
    {
        private readonly ComponentesDatos componentesDatos = new ComponentesDatos();
        private readonly EnsambleDatos ensambleDatos = new EnsambleDatos();
        private readonly SecuenciaEnsambleDatos secuenciaDatos = new SecuenciaEnsambleDatos();
        private ObservableCollection<SecuenciaEnsamble> secuencia = new ObservableCollection<SecuenciaEnsamble>();

        public NuevoEnsambleVistaPagina()
        {
            InitializeComponent();
            CargarTipos();
            GridSecuencia.ItemsSource = secuencia;
        }

        public NuevoEnsambleVistaPagina(Ensamble ensamble, List<SecuenciaEnsamble> secuenciaExistente)
        {
            InitializeComponent();
            CargarTipos();

            TxtNombreEnsamble.Text = ensamble.Nombre;
            TxtNombreEnsamble.IsEnabled = false;
            TxtOperadoresDefault.Text = ensamble.NumeroOperadoresDefault.ToString();
            CmbUnidadTrabajo.Text = ensamble.UnidadTrabajo;

            foreach (var item in secuenciaExistente)
                secuencia.Add(item);

            GridSecuencia.ItemsSource = secuencia;
        }

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
            string especificacion = TxtFiltroEspecificacion.Text.Trim();
            ListaComponentes.ItemsSource = componentesDatos.ObtenerPorFiltro(tipo, especificacion);
        }

        private void CmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CargarComponentesFiltrados();
        }

        private void TxtFiltroEspecificacion_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarComponentesFiltrados();
        }

        private void AgregarComponenteSecuencia_Click(object sender, RoutedEventArgs e)
        {
            if (ListaComponentes.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un componente de la lista.");
                return;
            }

            if (!decimal.TryParse(TxtTiempo.Text, out decimal tiempo) || tiempo <= 0)
            {
                MessageBox.Show("Ingresa un tiempo válido en segundos.");
                return;
            }

            if (!int.TryParse(TxtEstacion.Text, out int estacion) || estacion <= 0)
            {
                MessageBox.Show("Ingresa un número de estación válido.");
                return;
            }

            Componente seleccionado = ListaComponentes.SelectedItem as Componente;

            secuencia.Add(new SecuenciaEnsamble
            {
                OrdenSecuencia = secuencia.Count + 1,
                ComponenteID = seleccionado.ComponenteID,
                NombreComponente = seleccionado.Nombre,
                EspecificacionComponente = seleccionado.Descripcion,
                TiempoEstimado = tiempo,
                EstacionAsignada = estacion
            });

            TxtTiempo.Text = string.Empty;
            TxtEstacion.Text = string.Empty;
        }

        private void QuitarComponenteSecuencia_Click(object sender, RoutedEventArgs e)
        {
            if (GridSecuencia.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un componente de la secuencia.");
                return;
            }

            secuencia.Remove(GridSecuencia.SelectedItem as SecuenciaEnsamble);
            RecalcularOrden();
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

        private void GuardarEnsamble_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNombreEnsamble.Text))
            {
                MessageBox.Show("Ingresa el nombre del ensamble.");
                return;
            }

            if (CmbUnidadTrabajo.SelectedItem == null)
            {
                MessageBox.Show("Selecciona la unidad de trabajo.");
                return;
            }

            if (!int.TryParse(TxtOperadoresDefault.Text, out int operadoresDefault) || operadoresDefault <= 0)
            {
                MessageBox.Show("Ingresa un número de operadores válido.");
                return;
            }

            if (secuencia.Count == 0)
            {
                MessageBox.Show("Agrega al menos un componente a la secuencia.");
                return;
            }

            // Validar que todas las estaciones del 1 al N tengan al menos un componente
            var estacionesUsadas = secuencia.Select(s => s.EstacionAsignada).Distinct().OrderBy(x => x).ToList();
            var estacionesEsperadas = Enumerable.Range(1, operadoresDefault).ToList();

            var fueraDeRango = estacionesUsadas.Except(estacionesEsperadas).ToList();
            if (fueraDeRango.Any())
            {
                MessageBox.Show($"Existen componentes asignados a estaciones inválidas: {string.Join(", ", fueraDeRango)}. El máximo permitido es {operadoresDefault} estaciones.");
                return;
            }

            var faltantes = estacionesEsperadas.Except(estacionesUsadas).ToList();
            if (faltantes.Any())
            {
                MessageBox.Show($"Faltan componentes en las estaciones: {string.Join(", ", faltantes)}. La receta debe tener componentes en todas las estaciones del 1 al {operadoresDefault}.");
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
                MessageBox.Show($"Ya existe un ensamble con el nombre '{ensamble.Nombre}'. Elige otro nombre.");
                return;
            }
            try
            {
                secuenciaDatos.EliminarPorEnsamble(ensamble.Nombre); // Eliminar secuencia existente para evitar duplicados

                if (TxtNombreEnsamble.IsEnabled)
                    ensambleDatos.Agregar(ensamble);
                else
                    ensambleDatos.Actualizar(ensamble);

                List<SecuenciaEnsamble> lista = new List<SecuenciaEnsamble>();
                foreach (var item in secuencia)
                {
                    lista.Add(new SecuenciaEnsamble
                    {
                        EnsambleNombre = ensamble.Nombre,
                        ComponenteID = item.ComponenteID,
                        OrdenSecuencia = item.OrdenSecuencia,
                        TiempoEstimado = item.TiempoEstimado,
                        NombreComponente = item.NombreComponente,
                        EspecificacionComponente = item.EspecificacionComponente,
                        EstacionAsignada = item.EstacionAsignada
                    });
                }

                secuenciaDatos.AgregarLista(lista);
                MessageBox.Show("Ensamble guardado correctamente.");
                secuencia.Clear();
                TxtNombreEnsamble.Text = string.Empty;
                TxtNombreEnsamble.IsEnabled = true;
                TxtOperadoresDefault.Text = string.Empty;
                CmbUnidadTrabajo.SelectedItem = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el ensamble: {ex.Message}");
            }
        }
    }
}
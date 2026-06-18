using SmartBins.Datos;
using SmartBins.Modelos;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SmartBins.Vistas
{
    public partial class CatalogosVistaPagina : Page
    {
        private readonly BinDatos binDatos = new BinDatos();
        private readonly ComponentesDatos componentesDatos = new ComponentesDatos();
        private Bin binSeleccionado = null;
        private Componente componenteSeleccionado = null;

        public CatalogosVistaPagina()
        {
            InitializeComponent();
            CargarBins();
            CargarComponentes();
            CargarTiposComponente();
        }

        // ===================== BINS =====================

        private void CargarBins()
        {
            GridBins.ItemsSource = binDatos.ObtenerTodos();
        }

        private void AgregarBin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtBinCodigo.Text) ||
                string.IsNullOrWhiteSpace(TxtBinDescripcion.Text))
            {
                MessageBox.Show("Por favor completa todos los campos.");
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
                MessageBox.Show("Selecciona un bin de la lista primero.");
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
                MessageBox.Show("Selecciona un bin de la lista primero.");
                return;
            }

            MessageBoxResult resultado = MessageBox.Show(
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
            GridComponentes.ItemsSource = componentesDatos.ObtenerTodos();
        }

        private void CargarTiposComponente()
        {
            CmbNombreComponente.ItemsSource = componentesDatos.ObtenerTipos();
        }

        private void CmbNombreComponente_GotFocus(object sender, RoutedEventArgs e)
        {
            CargarTiposComponente();
        }

        private void AgregarComponente_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CmbNombreComponente.Text))
            {
                MessageBox.Show("El nombre del componente es obligatorio.");
                return;
            }

            Componente nuevo = new Componente
            {
                Nombre = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CmbNombreComponente.Text.Trim().ToLower()),
                Descripcion = TxtComponenteDescripcion.Text.Trim()
            };

            componentesDatos.Agregar(nuevo);
            CargarComponentes();
            CargarTiposComponente();
            LimpiarComponente();
        }

        private void ActualizarComponente_Click(object sender, RoutedEventArgs e)
        {
            if (componenteSeleccionado == null)
            {
                MessageBox.Show("Selecciona un componente de la lista primero.");
                return;
            }

            componenteSeleccionado.Nombre = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CmbNombreComponente.Text.Trim().ToLower());
            componenteSeleccionado.Descripcion = TxtComponenteDescripcion.Text.Trim();
            componentesDatos.Actualizar(componenteSeleccionado);
            CargarComponentes();
            CargarTiposComponente();
            LimpiarComponente();
        }

        private void EliminarComponente_Click(object sender, RoutedEventArgs e)
        {
            if (componenteSeleccionado == null)
            {
                MessageBox.Show("Selecciona un componente de la lista primero.");
                return;
            }

            MessageBoxResult resultado = MessageBox.Show(
                $"¿Eliminar el componente {componenteSeleccionado.Nombre}?",
                "Confirmar", MessageBoxButton.YesNo);

            if (resultado == MessageBoxResult.Yes)
            {
                componentesDatos.Eliminar(componenteSeleccionado.ComponenteID);
                CargarComponentes();
                CargarTiposComponente();
                LimpiarComponente();
            }
        }

        private void LimpiarComponente_Click(object sender, RoutedEventArgs e) => LimpiarComponente();

        private void LimpiarComponente()
        {
            CmbNombreComponente.Text = string.Empty;
            TxtComponenteDescripcion.Text = string.Empty;
            componenteSeleccionado = null;
            GridComponentes.SelectedItem = null;
        }

        private void GridComponentes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            componenteSeleccionado = GridComponentes.SelectedItem as Componente;
            if (componenteSeleccionado != null)
            {
                CmbNombreComponente.Text = componenteSeleccionado.Nombre;
                TxtComponenteDescripcion.Text = componenteSeleccionado.Descripcion;
            }
        }
    }
}
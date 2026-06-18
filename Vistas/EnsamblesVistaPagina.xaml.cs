using SmartBins.Datos;
using SmartBins.Modelos;
using System.Windows;
using System.Windows.Controls;

namespace SmartBins.Vistas
{
    public partial class EnsamblesVistaPagina : Page
    {
        private readonly EnsambleDatos ensambleDatos = new EnsambleDatos();
        private readonly SecuenciaEnsambleDatos secuenciaDatos = new SecuenciaEnsambleDatos();
        private Ensamble ensambleSeleccionado = null;

        public EnsamblesVistaPagina()
        {
            InitializeComponent();
            CmbFiltroUnidad.SelectedIndex = 0;
            CargarEnsambles();
        }

        private void CargarEnsambles()
        {
            if (ListaEnsambles == null) return;

            string nombre = TxtBuscar?.Text.Trim();
            string unidad = (CmbFiltroUnidad?.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (unidad == "Todas") unidad = null;
            ListaEnsambles.ItemsSource = ensambleDatos.ObtenerPorFiltro(nombre, unidad);
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarEnsambles();
        }

        private void CmbFiltroUnidad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CargarEnsambles();
        }

        private void ListaEnsambles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ensambleSeleccionado = ListaEnsambles.SelectedItem as Ensamble;
            if (ensambleSeleccionado != null)
            {
                TxtNombreEnsamble.Text = ensambleSeleccionado.Nombre;
                TxtTiempoTotal.Text = $"Tiempo estimado total: {ensambleSeleccionado.TiempoEstimado} seg  |  Operadores default: {ensambleSeleccionado.NumeroOperadoresDefault}  |  Unidad: {ensambleSeleccionado.UnidadTrabajo}";
                GridSecuencia.ItemsSource = secuenciaDatos.ObtenerPorEnsamble(ensambleSeleccionado.Nombre);
            }
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (ensambleSeleccionado == null)
            {
                MessageBox.Show("Selecciona un ensamble de la lista primero.");
                return;
            }

            MessageBoxResult resultado = MessageBox.Show(
                $"¿Eliminar el ensamble {ensambleSeleccionado.Nombre} y toda su secuencia?",
                "Confirmar", MessageBoxButton.YesNo);

            if (resultado == MessageBoxResult.Yes)
            {
                secuenciaDatos.EliminarPorEnsamble(ensambleSeleccionado.Nombre);
                ensambleDatos.Eliminar(ensambleSeleccionado.Nombre);
                GridSecuencia.ItemsSource = null;
                TxtNombreEnsamble.Text = string.Empty;
                TxtTiempoTotal.Text = string.Empty;
                ensambleSeleccionado = null;
                CargarEnsambles();
            }
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (ensambleSeleccionado == null)
            {
                MessageBox.Show("Selecciona un ensamble de la lista primero.");
                return;
            }

            var secuencia = secuenciaDatos.ObtenerPorEnsamble(ensambleSeleccionado.Nombre);
            NavigationService.Navigate(new NuevoEnsambleVistaPagina(ensambleSeleccionado, secuencia));
        }
    }
}
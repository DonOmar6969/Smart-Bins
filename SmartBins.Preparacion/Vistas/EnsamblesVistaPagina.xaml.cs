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
            var ensambles = ensambleDatos.ObtenerPorFiltro(nombre, unidad);
            ListaEnsambles.ItemsSource = ensambles;
            TxtTotalEnsambles.Text = ensambles.Count.ToString();
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
                PanelSinSeleccion.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtNombreEnsamble.Text = "Detalle del ensamble";
                TxtTiempoTotal.Text = "Selecciona un ensamble para consultar sus datos.";
                GridSecuencia.ItemsSource = null;
                PanelSinSeleccion.Visibility = Visibility.Visible;
            }
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (ensambleSeleccionado == null)
            {
                Servicios.DialogoSmart.Mostrar("Selecciona un ensamble de la lista primero.");
                return;
            }

            MessageBoxResult resultado = Servicios.DialogoSmart.Mostrar(
                $"¿Eliminar el ensamble {ensambleSeleccionado.Nombre} y toda su secuencia?",
                "Confirmar", MessageBoxButton.YesNo);

            if (resultado == MessageBoxResult.Yes)
            {
                var pasosEliminados =
                    secuenciaDatos.ObtenerPorEnsamble(ensambleSeleccionado.Nombre);
                secuenciaDatos.EliminarPorEnsamble(ensambleSeleccionado.Nombre);
                ensambleDatos.Eliminar(ensambleSeleccionado.Nombre);
                int imagenesEliminadas =
                    ImagenesArchivosService.EliminarImagenesDeSecuencia(pasosEliminados);
                GridSecuencia.ItemsSource = null;
                TxtNombreEnsamble.Text = "Detalle del ensamble";
                TxtTiempoTotal.Text = "Selecciona un ensamble para consultar sus datos.";
                PanelSinSeleccion.Visibility = Visibility.Visible;
                ensambleSeleccionado = null;
                CargarEnsambles();
                Servicios.DialogoSmart.Mostrar(
                    $"Ensamble eliminado correctamente. Se eliminaron " +
                    $"{imagenesEliminadas} imagen(es) asociadas.");
            }
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (ensambleSeleccionado == null)
            {
                Servicios.DialogoSmart.Mostrar("Selecciona un ensamble de la lista primero.");
                return;
            }

            var secuencia = secuenciaDatos.ObtenerPorEnsamble(ensambleSeleccionado.Nombre);
            NavigationService.Navigate(new NuevoEnsambleVistaPagina(ensambleSeleccionado, secuencia));
        }
    }
}

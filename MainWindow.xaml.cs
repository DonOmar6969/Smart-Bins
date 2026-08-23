using System.Windows;

namespace SmartBins
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Navegar(new Vistas.InicioVistaPagina(), "Inicio");
        }

        private void Inicio_Click(object sender, RoutedEventArgs e) =>
            Navegar(new Vistas.InicioVistaPagina(), "Inicio");

        private void Produccion_Click(object sender, RoutedEventArgs e)
        {
            Navegar(new Vistas.ProduccionSupervisorVistaPagina(), "Produccion");
        }
        private void Configuracion_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Vistas.ConfiguracionVistaPagina { Owner = this };
            ventana.ShowDialog();
        }

        private void Navegar(System.Windows.Controls.Page pagina, string seccion)
        {
            AreaContenido.Navigate(pagina);
            foreach (System.Windows.Controls.Button boton in
                     MenuExpandido.Children.OfType<System.Windows.Controls.Button>()
                         .Concat(PieMenuExpandido.Child is System.Windows.Controls.Panel panelExpandido
                             ? panelExpandido.Children.OfType<System.Windows.Controls.Button>()
                             : Enumerable.Empty<System.Windows.Controls.Button>()))
            {
                bool activo = string.Equals(boton.Tag?.ToString(), seccion, StringComparison.Ordinal);
                boton.Background = activo
                    ? new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0, 74, 148))
                    : System.Windows.Media.Brushes.Transparent;
                boton.FontWeight = activo ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        private void AcercaDe_Click(object sender, RoutedEventArgs e)
        {
            Servicios.DialogoSmart.Mostrar(
                "Smart Bins\nVersión 1.0\n\nSistema para la gestión de inventario, ensambles y producción.\nDesarrollado para EATON.",
                "Acerca de Smart Bins",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AreaContenido_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

        }
    }
}

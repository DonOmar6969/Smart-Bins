using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SmartBins.Preparacion.Datos;
using SmartBins.Preparacion.Modelos;

namespace SmartBins.Preparacion;

public partial class MainWindow : Window
{
    private readonly PreparacionDatos datos = new();
    private List<ElementoPreparacion> plan = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void Preparacion_Click(object sender, RoutedEventArgs e)
    {
        AreaAdministracion.Visibility = Visibility.Collapsed;
        ContenidoPreparacion.Visibility = Visibility.Visible;
    }

    private void Inventario_Click(object sender, RoutedEventArgs e) =>
        NavegarAdministracion(new global::SmartBins.Vistas.InventarioVistaPagina());

    private void Ensambles_Click(object sender, RoutedEventArgs e) =>
        NavegarAdministracion(new global::SmartBins.Vistas.EnsamblesVistaPagina());

    private void NuevoEnsamble_Click(object sender, RoutedEventArgs e) =>
        NavegarAdministracion(new global::SmartBins.Vistas.NuevoEnsambleVistaPagina());

    private void NavegarAdministracion(Page pagina)
    {
        ContenidoPreparacion.Visibility = Visibility.Collapsed;
        AreaAdministracion.Visibility = Visibility.Visible;
        AreaAdministracion.Navigate(pagina);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            CmbEnsambles.ItemsSource = await datos.ObtenerEnsamblesAsync();
        }
        catch (Exception ex)
        {
            TxtError.Text = $"No fue posible conectar con SmartBins. {ex.Message}";
        }
    }

    private void CmbEnsambles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbEnsambles.SelectedItem is not EnsambleOpcion ensamble) return;
        TxtResumenEnsamble.Text = $"Unidad de trabajo: {Valor(ensamble.UnidadTrabajo)}   ·   Estaciones configuradas: {ensamble.NumeroEstaciones}";
        TarjetaResumen.Visibility = Visibility.Visible;
        BtnComenzar.IsEnabled = ensamble.NumeroEstaciones > 0;
        TxtError.Text = ensamble.NumeroEstaciones > 0 ? string.Empty : "Este ensamble aún no tiene estaciones asignadas.";
    }

    private async void BtnComenzar_Click(object sender, RoutedEventArgs e)
    {
        if (CmbEnsambles.SelectedItem is not EnsambleOpcion ensamble) return;
        BtnComenzar.IsEnabled = false;
        try
        {
            plan = await datos.CrearPlanAsync(ensamble.Nombre);
            if (plan.Count == 0) throw new InvalidOperationException("El ensamble no contiene componentes para preparar.");

            TxtEnsambleActivo.Text = ensamble.Descripcion;
            ListaEstaciones.ItemsSource = plan.Select(x => x.Estacion).Distinct().OrderBy(x => x)
                .Select(x => $"Estación {x}").ToList();
            PanelSeleccion.Visibility = Visibility.Collapsed;
            PanelGuia.Visibility = Visibility.Visible;
            ListaEstaciones.SelectedIndex = 0;
            ActualizarProgreso();
        }
        catch (Exception ex)
        {
            TxtError.Text = ex.Message;
        }
        finally
        {
            BtnComenzar.IsEnabled = true;
        }
    }

    private void ListaEstaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListaEstaciones.SelectedIndex < 0) return;
        int estacion = plan.Select(x => x.Estacion).Distinct().OrderBy(x => x).ElementAt(ListaEstaciones.SelectedIndex);
        TxtTituloEstacion.Text = $"Preparar estación {estacion}";
        ListaComponentes.ItemsSource = new ObservableCollection<ElementoPreparacion>(plan.Where(x => x.Estacion == estacion));
    }

    private void Confirmacion_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { Tag: ElementoPreparacion item } check)
            item.Confirmado = check.IsChecked == true;
        ActualizarProgreso();
    }

    private void ActualizarProgreso()
    {
        int confirmados = plan.Count(x => x.Confirmado);
        BarraProgreso.Value = plan.Count == 0 ? 0 : confirmados * 100d / plan.Count;
        TxtProgreso.Text = $"{confirmados} de {plan.Count} bins cargados";
        BtnFinalizar.IsEnabled = plan.Count > 0 && confirmados == plan.Count;
    }

    private void BtnCambiar_Click(object sender, RoutedEventArgs e)
    {
        plan.Clear();
        PanelGuia.Visibility = Visibility.Collapsed;
        PanelSeleccion.Visibility = Visibility.Visible;
        CmbEnsambles.SelectedIndex = -1;
        TarjetaResumen.Visibility = Visibility.Collapsed;
        BtnComenzar.IsEnabled = false;
    }

    private void BtnFinalizar_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Todas las estaciones están preparadas y listas para producción.",
            "Preparación completada", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string Valor(string valor) => string.IsNullOrWhiteSpace(valor) ? "No especificada" : valor;
}

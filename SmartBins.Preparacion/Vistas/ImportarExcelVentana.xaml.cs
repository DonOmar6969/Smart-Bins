using SmartBins.Datos;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SmartBins.Vistas
{
    public partial class ImportarExcelVentana : Window
    {
        public List<FilaImportada> FilasConfirmadas { get; private set; } = new();
        private ObservableCollection<FilaImportada> _filas;

        public ImportarExcelVentana(ResultadoImportacion resultado)
        {
            InitializeComponent();

            _filas = new ObservableCollection<FilaImportada>(resultado.Filas);
            GridPreview.ItemsSource = _filas;

            TxtInfoHoja.Text = $"Hoja: \"{resultado.HojaUsada}\"  —  {_filas.Count} componentes detectados";
            TxtInfo2.Text = $"Filas descartadas (subensambles / phantoms): {resultado.FilasDescartadas}  " +
                            $"  |  Tip: edita la columna \"Estación\" para asignar operadores antes de cargar.";

            if (resultado.Advertencias.Any())
            {
                PanelAdvertencias.Visibility = Visibility.Visible;
                TxtAdvertencias.Text = "⚠  " + string.Join("\n⚠  ", resultado.Advertencias);
            }

            ActualizarConteo();
        }

        private void ActualizarConteo()
        {
            int sel = _filas.Count(f => f.Seleccionada);
            TxtConteo.Text = $"{sel} de {_filas.Count} filas seleccionadas";
        }

        private void SeleccionarTodo_Click(object sender, RoutedEventArgs e)
        {
            foreach (var f in _filas) f.Seleccionada = true;
            GridPreview.Items.Refresh();
            ActualizarConteo();
        }

        private void DeseleccionarTodo_Click(object sender, RoutedEventArgs e)
        {
            foreach (var f in _filas) f.Seleccionada = false;
            GridPreview.Items.Refresh();
            ActualizarConteo();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CargarSeleccionados_Click(object sender, RoutedEventArgs e)
        {
            FilasConfirmadas = _filas.Where(f => f.Seleccionada).ToList();

            if (!FilasConfirmadas.Any())
            {
                Servicios.DialogoSmart.Mostrar("Selecciona al menos una fila para cargar.", "Sin selección",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar que todas tengan estación asignada
            var sinEstacion = FilasConfirmadas.Where(f => f.EstacionAsignada <= 0).ToList();
            if (sinEstacion.Any())
            {
                var resultado = Servicios.DialogoSmart.Mostrar(
                    $"{sinEstacion.Count} fila(s) no tienen estación asignada. Se les asignará la estación 1 por defecto.\n\n¿Continuar?",
                    "Estaciones sin asignar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.No) return;

                foreach (var f in sinEstacion)
                    f.EstacionAsignada = 1;
            }

            DialogResult = true;
            Close();
        }
    }
}

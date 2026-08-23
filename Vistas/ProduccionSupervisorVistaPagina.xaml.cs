using SmartBins.Datos;
using SmartBins.Modelos;
using SmartBins.Modelos.Produccion;
using SmartBins.Servicios.Balanceo;
using SmartBins.Servicios.Produccion;
using SmartBins.Servicios.Reportes;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SmartBins.Vistas
{
    public partial class ProduccionSupervisorVistaPagina : Page
    {
        private readonly EnsambleDatos ensambleDatos = new();
        private readonly SecuenciaEnsambleDatos secuenciaDatos = new();
        private readonly ComponentesDatos componentesDatos = new();
        private readonly ILineBalancingService balanceo = new LineBalancingService();
        private readonly IReporteLiberacionPdfService reportePdf =
            new ReporteLiberacionPdfService();
        private PlanBalanceo? planActual;

        private bool ValidarEnsambleSeleccionado(Ensamble ensamble)
        {
            var resultado = Servicios.ValidacionEnsambleService.Validar(
                ensamble,
                secuenciaDatos.ObtenerPorEnsamble(ensamble.Nombre),
                componentesDatos.ObtenerTodos());
            if (resultado.EsValido) return true;

            Servicios.DialogoSmart.Mostrar(
                resultado.CrearResumen(),
                "Ensamble incompleto",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        public ProduccionSupervisorVistaPagina()
        {
            InitializeComponent();
            Servicios.EntradaNumericaService.RestringirAEnteros(TxtOperadores, TxtCantidadUnidades);
            var configuracion = Servicios.ConfiguracionAplicacionService.Cargar();
            TxtOperadores.Text = configuracion.OperadoresPredeterminados.ToString();
            TxtCantidadUnidades.Text = configuracion.UnidadesPruebaPredeterminadas.ToString();
            CmbEnsamble.ItemsSource = ensambleDatos.ObtenerTodos();
        }

        private void CmbEnsamble_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            planActual = null;
            BtnIniciarCorrida.IsEnabled = false;
            BtnDescargarReporte.IsEnabled = false;
            if (CmbEnsamble.SelectedItem is Ensamble ensamble)
            {
                int operadoresPredeterminados = ensamble.NumeroOperadoresDefault > 0
                    ? ensamble.NumeroOperadoresDefault
                    : Servicios.ConfiguracionAplicacionService.Cargar().OperadoresPredeterminados;
                TxtOperadores.Text = operadoresPredeterminados.ToString();
            }
            TxtResumenBalance.Text = "Presiona “Calcular balance” para preparar la corrida.";
        }

        private void CalcularBalance_Click(object sender, RoutedEventArgs e)
        {
            if (CmbEnsamble.SelectedItem is not Ensamble ensamble)
            {
                Servicios.DialogoSmart.Mostrar("Selecciona un ensamble.", "Producción",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var secuencia = secuenciaDatos.ObtenerPorEnsamble(ensamble.Nombre);
            if (secuencia.Count == 0)
            {
                Servicios.DialogoSmart.Mostrar("El ensamble seleccionado no contiene operaciones.",
                    "Producción", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!int.TryParse(TxtOperadores.Text, out int operadores) || operadores <= 0)
                {
                    Servicios.DialogoSmart.Mostrar("Ingresa una cantidad de operadores válida.");
                    return;
                }

                planActual = balanceo.Calcular(
                    ensamble.Nombre, secuencia, componentesDatos.ObtenerTodos(), operadores,
                    Servicios.ConfiguracionAplicacionService.CrearConfiguracionBalanceo());

                string distribucion = string.Join("\n", planActual.Estaciones.Select(e =>
                    $"Estación {e.Numero}: {e.Operaciones.Count} operaciones, " +
                    $"{e.CargaTotalSegundos:0.##} segundos"));
                TxtResumenBalance.Text =
                    $"Operaciones: {planActual.Estaciones.Sum(e => e.Operaciones.Count)}\n" +
                    $"Estaciones: {planActual.NumeroOperadores}\n" +
                    $"Tiempo objetivo por estación: {planActual.TiempoObjetivoPorEstacion:0.##} segundos\n" +
                    $"Tiempo de ciclo estimado: {planActual.TiempoCicloLinea:0.##} segundos\n" +
                    $"Tipo de mezcla: {(planActual.EsMezclaAlta ? "alta (repeticiones conservadas)" : "normal")}\n\n" +
                    distribucion +
                    (planActual.Recomendaciones.Count == 0
                        ? "\n\nSin riesgos de confusión entre números de parte."
                        : "\n\nRECOMENDACIONES\n• " + string.Join("\n• ", planActual.Recomendaciones));
                BtnIniciarCorrida.IsEnabled = true;
                BtnDescargarReporte.IsEnabled = true;
            }
            catch (Exception ex)
            {
                planActual = null;
                BtnIniciarCorrida.IsEnabled = false;
                BtnDescargarReporte.IsEnabled = false;
                Servicios.DialogoSmart.Mostrar(ex.Message, "No se pudo calcular el balance",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DescargarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (planActual == null || CmbEnsamble.SelectedItem is not Ensamble ensamble)
            {
                Servicios.DialogoSmart.Mostrar("Calcula el balance antes de generar el reporte.");
                return;
            }
            if (!ValidarEnsambleSeleccionado(ensamble)) return;

            if (!int.TryParse(TxtCantidadUnidades.Text, out int cantidad) || cantidad <= 0)
            {
                Servicios.DialogoSmart.Mostrar("Ingresa una cantidad de unidades válida.");
                return;
            }

            string nombreSeguro = string.Concat(ensamble.Nombre.Select(c =>
                Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            var dialogo = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Guardar reporte de liberación",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                FileName = $"Reporte_Liberacion_{nombreSeguro}_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (dialogo.ShowDialog() != true) return;

            try
            {
                reportePdf.Generar(dialogo.FileName, new ReporteLiberacionDatos
                {
                    Modelo = ensamble.Nombre,
                    Linea = ensamble.UnidadTrabajo,
                    FechaGeneracion = DateTime.Now,
                    CantidadUnidades = cantidad,
                    Plan = planActual
                });
                Servicios.ControlEnsambleService.RegistrarLiberacion(ensamble.Nombre);

                Servicios.DialogoSmart.Mostrar(
                    $"Reporte generado correctamente:\n{dialogo.FileName}",
                    "Reporte de liberación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Servicios.DialogoSmart.Mostrar(
                    $"No se pudo generar el reporte:\n{ex.Message}",
                    "Reporte de liberación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void IniciarCorrida_Click(object sender, RoutedEventArgs e)
        {
            if (planActual == null)
            {
                Servicios.DialogoSmart.Mostrar("Calcula el balance antes de iniciar.");
                return;
            }
            if (CmbEnsamble.SelectedItem is not Ensamble ensamble ||
                !ValidarEnsambleSeleccionado(ensamble))
                return;

            if (!int.TryParse(TxtCantidadUnidades.Text, out int cantidad) || cantidad <= 0)
            {
                Servicios.DialogoSmart.Mostrar("Ingresa una cantidad de unidades válida.");
                return;
            }

            // Crea un solo coordinador para que todas las estaciones compartan el avance de las unidades.
            var corridaCompartida = new CorridaProduccionCompartida(planActual, cantidad);
            corridaCompartida.CorridaFinalizada += CorridaCompartida_CorridaFinalizada;

            // Recorre las sesiones que el coordinador creó en el orden de la línea.
            foreach (CorridaProduccionEnMemoria corrida in corridaCompartida.Estaciones)
            {
                // Crea la entrada independiente que recibirá el botón o la tecla F8 de este operador.
                var entrada = new WpfStationInputService();

                // Construye una ventana conectada a la sesión compartida de esta estación.
                var ventana = new EstacionOperadorVentana(corrida, entrada)
                {
                    // Mantiene la ventana principal como propietaria de las ventanas de operador.
                    Owner = Window.GetWindow(this)
                };

                // Muestra la estación sin bloquear la apertura de las demás estaciones.
                ventana.Show();
            }
        }

        private void CorridaCompartida_CorridaFinalizada(
            object? sender,
            ResultadoCorridaProduccion resultado)
        {
            Dispatcher.BeginInvoke(() =>
            {
                string estaciones = string.Join("\n", resultado.Estaciones.Select(e =>
                    $"Estación {e.NumeroEstacion}: {e.OperacionesCompletadas} operaciones · " +
                    $"{FormatearDuracion(e.Duracion)}"));
                string resumen =
                    $"CORRIDA FINALIZADA\n\n" +
                    $"Ensamble: {resultado.Ensamble}\n" +
                    $"Unidades terminadas: {resultado.CantidadUnidades}\n" +
                    $"Operaciones completadas: {resultado.OperacionesCompletadas}\n" +
                    $"Duración total: {FormatearDuracion(resultado.DuracionTotal)}\n" +
                    $"Tiempo de ciclo real: {resultado.TiempoCicloRealSegundos:0.0} s/unidad\n" +
                    $"Ritmo equivalente: {resultado.UnidadesPorHora:0.0} unidades/hora\n\n" +
                    estaciones;

                TxtResumenBalance.Text = resumen;
                Servicios.DialogoSmart.Mostrar(
                    resumen,
                    "Resultados de la corrida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        private static string FormatearDuracion(TimeSpan tiempo) =>
            tiempo.TotalHours >= 1
                ? tiempo.ToString(@"hh\:mm\:ss")
                : tiempo.ToString(@"mm\:ss\.f");
    }
}

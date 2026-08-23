using SmartBins.Modelos.Produccion;
using SmartBins.Servicios.Produccion;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SmartBins.Vistas
{
    public partial class EstacionOperadorVentana : Window
    {
        private readonly CorridaProduccionEnMemoria corrida;
        private readonly IStationInputService entrada;
        private readonly DispatcherTimer temporizador;

        public EstacionOperadorVentana(
            CorridaProduccionEnMemoria corrida,
            IStationInputService entrada)
        {
            InitializeComponent();
            this.corrida = corrida;
            this.entrada = entrada;
            Title = $"SMART BINS - Estación {corrida.NumeroEstacion}";
            TxtNumeroEstacion.Text = $"ESTACIÓN {corrida.NumeroEstacion}";
            // Escucha las solicitudes generadas por el botón o por la tecla F8.
            entrada.CompletarOperacionSolicitada += Entrada_CompletarOperacionSolicitada;

            // Escucha al coordinador para actualizar la pantalla cuando llegue una unidad.
            corrida.EstadoCambiado += Corrida_EstadoCambiado;

            temporizador = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            temporizador.Tick += Temporizador_Tick;

            Closed += (_, _) =>
            {
                // Detiene la actualización visual del cronómetro al cerrar la ventana.
                temporizador.Stop();

                // Retira el manejador del dispositivo de entrada para evitar referencias pendientes.
                entrada.CompletarOperacionSolicitada -= Entrada_CompletarOperacionSolicitada;

                // Retira la ventana de las notificaciones de la corrida compartida.
                corrida.EstadoCambiado -= Corrida_EstadoCambiado;
            };

            MostrarOperacionActual();
        }

        private void MostrarOperacionActual()
        {
            OperacionPlanificada? operacion = corrida.OperacionActual;
            if (operacion == null)
            {
                MostrarCorridaFinalizada();
                return;
            }

            TxtEnsamble.Text = corrida.Plan.EnsambleNombre;
            TxtUnidad.Text = $"Unidad {corrida.UnidadActual:000} de {corrida.CantidadUnidades:000}";

            // Detecta si la estación todavía no recibe esta unidad desde la estación anterior.
            if (corrida.Estado == EstadoEstacion.EsperandoTransferencia)
            {
                // Sustituye los datos de operación por una pantalla informativa de espera.
                MostrarEsperaTransferencia();

                // Evita iniciar el cronómetro o habilitar el botón antes de recibir la unidad.
                return;
            }

            TxtEstado.Text = "Trabajando";
            TxtPaso.Text = $"Operación {corrida.IndiceOperacionActual + 1} de {corrida.OperacionesPorUnidad}";
            TxtNombreComponente.Text = operacion.NombreComponente;
            TxtNumeroParte.Text = string.IsNullOrWhiteSpace(operacion.NumeroParte)
                ? "Número de parte no especificado"
                : $"No. de parte: {operacion.NumeroParte}";
            TxtCantidad.Text = operacion.Cantidad.ToString();
            TxtReferencia.Text = ValorOAlternativa(operacion.Referencia);
            TxtBin.Text = ValorOAlternativa(operacion.Bin);
            TxtPolaridad.Text = ValorOAlternativa(operacion.Polaridad);
            TxtInstruccion.Text = ValorOAlternativa(operacion.Instruccion);
            TxtTiempoObjetivo.Text = FormatearSegundos((double)operacion.TiempoObjetivoSegundos);
            TxtTiempoTranscurrido.Text = "00:00.0";
            TxtDiferencia.Text = FormatearDiferencia(-(double)operacion.TiempoObjetivoSegundos);
            TxtDiferencia.Foreground = Brushes.SeaGreen;

            MostrarGaleriaImagenes(operacion);
            ActualizarProgreso();

            BtnCompletar.IsEnabled = true;
            BtnCompletar.Content = "COMPLETAR OPERACIÓN   (F8)";
            corrida.IniciarOperacion();
            temporizador.Start();
        }

        // Configura la interfaz mientras la estación anterior conserva la unidad requerida.
        private void MostrarEsperaTransferencia()
        {
            // Detiene el cronómetro porque todavía no existe una operación activa.
            temporizador.Stop();

            // Informa el estado general en la esquina superior de la ventana.
            TxtEstado.Text = "Esperando transferencia";

            // Reemplaza el número de operación con una indicación de unidad pendiente.
            TxtPaso.Text = "Unidad pendiente";

            // Muestra exactamente cuál unidad necesita recibir esta estación.
            TxtNombreComponente.Text =
                $"Esperando la unidad {corrida.UnidadActual:000} de la estación anterior";

            // Oculta el número de parte porque todavía no se presenta una operación.
            TxtNumeroParte.Text = string.Empty;

            // Explica al operador que la pantalla cambiará sin intervención manual.
            TxtInstruccion.Text =
                "La operación se habilitará automáticamente cuando la estación anterior libere la unidad.";

            // Sustituye la cantidad por un guion mientras no hay una operación disponible.
            TxtCantidad.Text = "—";

            // Sustituye la referencia por un guion mientras no hay una operación disponible.
            TxtReferencia.Text = "—";

            // Sustituye la ubicación del bin por un guion durante la espera.
            TxtBin.Text = "—";

            // Sustituye la polaridad por un guion durante la espera.
            TxtPolaridad.Text = "—";

            // Oculta el tiempo objetivo porque todavía no comienza la operación.
            TxtTiempoObjetivo.Text = "—";

            // Reinicia el tiempo visible para evitar conservar el de la unidad anterior.
            TxtTiempoTranscurrido.Text = "00:00.0";

            // Oculta la diferencia de tiempo porque no existe una medición activa.
            TxtDiferencia.Text = "—";

            // Limpia las imágenes de la operación anterior.
            LimpiarGaleriaImagenes();

            // Impide completar una operación mientras la unidad no esté disponible.
            BtnCompletar.IsEnabled = false;

            // Cambia el texto del botón para reforzar visualmente el estado de espera.
            BtnCompletar.Content = "ESPERANDO UNIDAD";

            // Conserva visible el porcentaje de trabajo terminado por esta estación.
            ActualizarProgreso();
        }

        // Recibe las notificaciones emitidas por la sesión de producción compartida.
        private void Corrida_EstadoCambiado(object? sender, EventArgs e)
        {
            // Comprueba si la notificación llegó desde un hilo diferente al de la ventana.
            if (!Dispatcher.CheckAccess())
            {
                // Traslada la actualización al hilo de interfaz requerido por WPF.
                Dispatcher.Invoke(MostrarOperacionActual);

                // Evita ejecutar la misma actualización una segunda vez.
                return;
            }

            // Actualiza directamente cuando ya se encuentra en el hilo de interfaz.
            MostrarOperacionActual();
        }

        private void CompletarOperacion_Click(object sender, RoutedEventArgs e)
            => entrada.SolicitarCompletarOperacion();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
            {
                entrada.SolicitarCompletarOperacion();
                e.Handled = true;
            }
        }

        private void Entrada_CompletarOperacionSolicitada(object? sender, EventArgs e)
        {
            // Detiene el cronómetro antes de cambiar de operación o unidad.
            temporizador.Stop();

            // Solicita al modelo que complete la operación únicamente si está trabajando.
            if (!corrida.CompletarOperacion())
            {
                // Reactiva el cronómetro si la solicitud no modificó una operación activa.
                if (corrida.Estado == EstadoEstacion.Trabajando)
                    // Continúa midiendo el tiempo de la misma operación.
                    temporizador.Start();

                // Sale porque no hubo ningún cambio que presentar.
                return;
            }
        }

        private void Temporizador_Tick(object? sender, EventArgs e)
        {
            OperacionPlanificada? operacion = corrida.OperacionActual;
            if (operacion == null) return;

            double transcurrido = corrida.TiempoTranscurridoOperacion.TotalSeconds;
            double diferencia = transcurrido - (double)operacion.TiempoObjetivoSegundos;
            TxtTiempoTranscurrido.Text = FormatearSegundos(transcurrido);
            TxtDiferencia.Text = FormatearDiferencia(diferencia);
            TxtDiferencia.Foreground = diferencia <= 0 ? Brushes.SeaGreen : Brushes.Firebrick;
        }

        private void ActualizarProgreso()
        {
            BarraProgreso.Value = corrida.OperacionesTotales == 0
                ? 100
                : 100.0 * corrida.OperacionesCompletadas / corrida.OperacionesTotales;
        }

        private void MostrarCorridaFinalizada()
        {
            temporizador.Stop();
            ActualizarProgreso();
            BarraProgreso.Value = 100;
            TxtEstado.Text = "Finalizada";
            TxtPaso.Text = "Corrida completada";
            TxtNombreComponente.Text = "Todas las operaciones fueron completadas";
            TxtNumeroParte.Text = string.Empty;
            TxtInstruccion.Text = "La corrida de prueba terminó correctamente.";
            TxtCantidad.Text = "—";
            TxtReferencia.Text = "—";
            TxtBin.Text = "—";
            TxtPolaridad.Text = "—";
            LimpiarGaleriaImagenes();
            BtnCompletar.IsEnabled = false;
            BtnCompletar.Content = "CORRIDA FINALIZADA";
        }

        private void MostrarGaleriaImagenes(OperacionPlanificada operacion)
        {
            var imagenes = new List<(string Etiqueta, string Ruta)>();

            if (!string.IsNullOrWhiteSpace(operacion.FotoComponenteRuta))
                imagenes.Add(("Componente", operacion.FotoComponenteRuta));

            string[] rutasProcedimiento = (operacion.ImagenProcedimientoRuta ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (int i = 0; i < rutasProcedimiento.Length; i++)
                imagenes.Add(($"Procedimiento {i + 1}", rutasProcedimiento[i]));

            var imagenesValidas = imagenes
                .Where(item => File.Exists(item.Ruta))
                .DistinctBy(item => item.Ruta, StringComparer.OrdinalIgnoreCase)
                .ToList();

            GaleriaImagenesProduccion.Children.Clear();
            GaleriaImagenesProduccion.RowDefinitions.Clear();
            GaleriaImagenesProduccion.ColumnDefinitions.Clear();
            TxtSinImagenesProduccion.Visibility =
                imagenesValidas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (imagenesValidas.Count == 0) return;

            int columnas = (int)Math.Ceiling(Math.Sqrt(imagenesValidas.Count));
            int filas = (int)Math.Ceiling((double)imagenesValidas.Count / columnas);

            for (int i = 0; i < columnas; i++)
                GaleriaImagenesProduccion.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < filas; i++)
                GaleriaImagenesProduccion.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < imagenesValidas.Count; i++)
            {
                ImageSource? source = CargarImagen(imagenesValidas[i].Ruta);
                if (source == null) continue;

                var tarjeta = new Border
                {
                    Margin = new Thickness(4),
                    Padding = new Thickness(5),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(228, 231, 236)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8)
                };

                var contenido = new Grid();
                contenido.RowDefinitions.Add(new RowDefinition
                    { Height = new GridLength(1, GridUnitType.Star) });
                contenido.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                contenido.Children.Add(new Image
                {
                    Source = source,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(2)
                });

                var etiqueta = new TextBlock
                {
                    Text = imagenesValidas[i].Etiqueta,
                    Margin = new Thickness(3, 3, 3, 1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(88, 112, 134)),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold
                };
                Grid.SetRow(etiqueta, 1);
                contenido.Children.Add(etiqueta);

                tarjeta.Child = contenido;
                Grid.SetColumn(tarjeta, i % columnas);
                Grid.SetRow(tarjeta, i / columnas);
                GaleriaImagenesProduccion.Children.Add(tarjeta);
            }
        }

        private void LimpiarGaleriaImagenes()
        {
            GaleriaImagenesProduccion.Children.Clear();
            GaleriaImagenesProduccion.RowDefinitions.Clear();
            GaleriaImagenesProduccion.ColumnDefinitions.Clear();
            TxtSinImagenesProduccion.Visibility = Visibility.Visible;
        }

        private static ImageSource? CargarImagen(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta)) return null;

            try
            {
                var imagen = new BitmapImage();
                imagen.BeginInit();
                imagen.UriSource = new Uri(ruta, UriKind.Absolute);
                imagen.CacheOption = BitmapCacheOption.OnLoad;
                imagen.EndInit();
                imagen.Freeze();
                return imagen;
            }
            catch
            {
                return null;
            }
        }

        private static string ValorOAlternativa(string valor)
            => string.IsNullOrWhiteSpace(valor) ? "No especificado" : valor;

        private static string FormatearSegundos(double segundos)
            => TimeSpan.FromSeconds(Math.Max(0, segundos)).ToString(@"mm\:ss\.f");

        private static string FormatearDiferencia(double segundos)
            => $"{(segundos >= 0 ? "+" : "−")}{FormatearSegundos(Math.Abs(segundos))}";
    }
}

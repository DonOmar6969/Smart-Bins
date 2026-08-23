using SmartBins.Modelos;
using SmartBins.Servicios;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace SmartBins.Vistas
{
    public partial class ConfiguracionVistaPagina : Window
    {
        private readonly ObservableCollection<int> nivelesTiempo = new();

        public ConfiguracionVistaPagina()
        {
            InitializeComponent();
            ListaNivelesTiempo.ItemsSource = nivelesTiempo;
            ConfigurarCamposNumericos();
            Mostrar(ConfiguracionAplicacionService.Cargar());
        }

        private void ConfigurarCamposNumericos()
        {
            TextBox[] campos =
            {
                TxtOperadores, TxtUnidadesPrueba, TxtNuevoTiempo, TxtUmbralRepeticiones,
                TxtPorcentajeMezcla, TxtPartesDistintas, TxtSufijoExacto,
                TxtSufijoParecido, TxtDiferencias
            };
            EntradaNumericaService.RestringirAEnteros(campos);
        }

        private void MenuSecciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelGeneral == null || PanelTiempos == null || PanelBalanceo == null) return;
            string seccion = (MenuSecciones.SelectedItem as ListBoxItem)?.Tag?.ToString() ?? "General";
            PanelGeneral.Visibility = seccion == "General" ? Visibility.Visible : Visibility.Collapsed;
            PanelTiempos.Visibility = seccion == "Tiempos" ? Visibility.Visible : Visibility.Collapsed;
            PanelBalanceo.Visibility = seccion == "Balanceo" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AgregarTiempo_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtNuevoTiempo.Text, out int segundos) || segundos < 1 || segundos > 30)
            {
                DialogoSmart.Mostrar("Ingresa un tiempo entero entre 1 y 30 segundos.",
                    "Tiempo no válido", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNuevoTiempo.Focus();
                return;
            }
            if (nivelesTiempo.Contains(segundos))
            {
                DialogoSmart.Mostrar("Ese nivel de tiempo ya existe.", "Nivel duplicado",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var ordenados = nivelesTiempo.Append(segundos).OrderBy(x => x).ToList();
            nivelesTiempo.Clear();
            foreach (int nivel in ordenados) nivelesTiempo.Add(nivel);
            TxtNuevoTiempo.Clear();
            TxtEstado.Text = string.Empty;
        }

        private void QuitarTiempo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: int nivel }) return;
            if (nivelesTiempo.Count == 1)
            {
                DialogoSmart.Mostrar("Debe existir al menos un nivel de tiempo.", "Acción no permitida",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            nivelesTiempo.Remove(nivel);
            TxtEstado.Text = string.Empty;
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configuracion = LeerFormulario();
                ConfiguracionAplicacionService.Guardar(configuracion);
                Mostrar(configuracion);
                TxtEstado.Text = "✓ Configuración guardada. Se aplicará en las nuevas pantallas y cálculos.";
            }
            catch (Exception ex)
            {
                DialogoSmart.Mostrar(ex.Message, "Configuración no válida",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Restablecer_Click(object sender, RoutedEventArgs e)
        {
            if (DialogoSmart.Mostrar("¿Restablecer todos los parámetros a sus valores originales?",
                "Restablecer configuración", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            ConfiguracionAplicacionService.Restablecer();
            Mostrar(ConfiguracionAplicacionService.Cargar());
            TxtEstado.Text = "✓ Se restablecieron los valores originales.";
        }

        private ConfiguracionAplicacion LeerFormulario() => new()
        {
            OperadoresPredeterminados = Entero(TxtOperadores.Text, "operadores predeterminados"),
            UnidadesPruebaPredeterminadas = Entero(TxtUnidadesPrueba.Text, "unidades de prueba"),
            NivelesTiempoCicloSegundos = nivelesTiempo.ToList(),
            UmbralRepeticionesParaDistribuir = Entero(TxtUmbralRepeticiones.Text, "repeticiones mínimas"),
            ProporcionMezclaAltaPorcentaje = Entero(TxtPorcentajeMezcla.Text, "porcentaje de mezcla alta"),
            MinimoPartesDistintasMezclaAlta = Entero(TxtPartesDistintas.Text, "partes distintas mínimas"),
            LongitudSufijoExacto = Entero(TxtSufijoExacto.Text, "caracteres finales exactos"),
            LongitudSufijoParecido = Entero(TxtSufijoParecido.Text, "caracteres finales parecidos"),
            DiferenciasPermitidasSufijo = Entero(TxtDiferencias.Text, "diferencias permitidas")
        };

        private void Mostrar(ConfiguracionAplicacion c)
        {
            TxtOperadores.Text = c.OperadoresPredeterminados.ToString();
            TxtUnidadesPrueba.Text = c.UnidadesPruebaPredeterminadas.ToString();
            nivelesTiempo.Clear();
            foreach (int nivel in c.NivelesTiempoCicloSegundos.OrderBy(x => x))
                nivelesTiempo.Add(nivel);
            TxtUmbralRepeticiones.Text = c.UmbralRepeticionesParaDistribuir.ToString();
            TxtPorcentajeMezcla.Text = c.ProporcionMezclaAltaPorcentaje.ToString();
            TxtPartesDistintas.Text = c.MinimoPartesDistintasMezclaAlta.ToString();
            TxtSufijoExacto.Text = c.LongitudSufijoExacto.ToString();
            TxtSufijoParecido.Text = c.LongitudSufijoParecido.ToString();
            TxtDiferencias.Text = c.DiferenciasPermitidasSufijo.ToString();
        }

        private static int Entero(string texto, string campo) =>
            int.TryParse(texto, out int valor) ? valor
                : throw new InvalidOperationException($"El campo {campo} debe contener un número entero.");
    }
}

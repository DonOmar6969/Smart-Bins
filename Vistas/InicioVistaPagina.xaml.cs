using SmartBins.Datos;
using SmartBins.Modelos;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SmartBins.Vistas
{
    public partial class InicioVistaPagina : Page
    {
        public InicioVistaPagina() => InitializeComponent();

        private void InicioVistaPagina_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Ensamble> ensambles = new EnsambleDatos().ObtenerTodos();
                List<Componente> componentes = new ComponentesDatos().ObtenerTodos();
                var secuencias = new SecuenciaEnsambleDatos();
                int desactualizados = componentes.Count(c => c.TiempoCiclo <= 0 ||
                    string.IsNullOrWhiteSpace(c.FotoRuta) || !File.Exists(c.FotoRuta) ||
                    !string.Equals(c.Estatus, "Actualizado", StringComparison.OrdinalIgnoreCase));
                int incompletos = ensambles.Count(e => secuencias.ObtenerPorEnsamble(e.Nombre).Count == 0);

                TxtEnsambles.Text = ensambles.Count.ToString();
                TxtComponentes.Text = componentes.Count.ToString();
                TxtDesactualizados.Text = desactualizados.ToString();
                TxtIncompletos.Text = incompletos.ToString();
                TxtEstado.Text = "● Base de datos conectada\nActualizado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                TxtRecomendacion.Text = desactualizados > 0
                    ? $"Actualizar {desactualizados} componente(s) antes de nuevas liberaciones."
                    : incompletos > 0 ? $"Revisar {incompletos} ensamble(s) sin operaciones."
                    : "No se detectaron pendientes críticos.";
            }
            catch (Exception ex)
            {
                TxtEstado.Text = "● No fue posible consultar la base de datos.\n" + ex.Message;
                TxtRecomendacion.Text = "Verifica la conexión antes de continuar.";
            }
        }
    }
}

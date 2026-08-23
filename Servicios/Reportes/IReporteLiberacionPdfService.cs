using SmartBins.Modelos.Produccion;

namespace SmartBins.Servicios.Reportes
{
    public class ReporteLiberacionDatos
    {
        public string Modelo { get; init; } = string.Empty;
        public string Linea { get; init; } = string.Empty;
        public DateTime FechaGeneracion { get; init; }
        public int CantidadUnidades { get; init; }
        public PlanBalanceo Plan { get; init; } = new();
    }

    public interface IReporteLiberacionPdfService
    {
        void Generar(string rutaArchivo, ReporteLiberacionDatos datos);
    }
}

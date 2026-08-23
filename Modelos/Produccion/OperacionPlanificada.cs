namespace SmartBins.Modelos.Produccion
{
    public class OperacionPlanificada
    {
        public int OperacionId { get; init; }
        public int Orden { get; init; }
        public int Estacion { get; init; }
        public int ComponenteID { get; init; }
        public string NombreComponente { get; init; } = string.Empty;
        public string NumeroParte { get; init; } = string.Empty;
        public string Instruccion { get; init; } = string.Empty;
        public string Referencia { get; init; } = string.Empty;
        public int Cantidad { get; init; } = 1;
        public decimal TiempoObjetivoSegundos { get; init; }
        public string FotoComponenteRuta { get; init; } = string.Empty;
        public string ImagenProcedimientoRuta { get; init; } = string.Empty;
        public string Bin { get; init; } = "No especificado";
        public string Polaridad { get; init; } = "No especificada";
    }
}

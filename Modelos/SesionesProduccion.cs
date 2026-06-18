namespace SmartBins.Modelos
{
    public class SesionesProduccion
    {
        // Cada propiedad corresponde a una columna en la tabla de la base de datos
        public int SesionID { get; set; }
        public string EnsambleNombre { get; set; } = string.Empty;
        public int NumeroOperadores { get; set; }
        public int CantidadEnsambles { get; set; }
        public int LineaID { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}

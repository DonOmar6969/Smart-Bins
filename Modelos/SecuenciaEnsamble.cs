namespace SmartBins.Modelos
{
    public class SecuenciaEnsamble
    {
        // Cada propiedad corresponde a una columna en la tabla de la base de datos
        public int SecuenciaID { get; set; }
        public string EnsambleNombre { get; set; } = string.Empty;
        public string NumeroParte { get; set; } = string.Empty;
        public string NombreComponente { get; set; } = string.Empty;
        public string EspecificacionComponente { get; set; } = string.Empty;
        public decimal TiempoEstimado { get; set; }
        public int ComponenteID { get; set; }
        public int OrdenSecuencia { get; set; }
        public int EstacionAsignada { get; set; } = 0;
        public string Referencia { get; set; } = string.Empty;
        public int OrdenPrecedente { get; set; }
        public string ImagenRuta { get; set; }
    }
}

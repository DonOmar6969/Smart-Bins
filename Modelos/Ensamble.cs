namespace SmartBins.Modelos
{
    // Cada propiedad corresponde a una columna en la tabla de la base de datos
    public class Ensamble
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal TiempoEstimado { get; set; }
        public string UnidadTrabajo { get; set; } = string.Empty;
        public int NumeroOperadoresDefault { get; set; }
    }
}

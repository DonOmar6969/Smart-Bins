namespace SmartBins.Modelos
{
    // Cada propiedad corresponde a una columna en la tabla de la base de datos
    public class Componente
    {
        public int ComponenteID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string FotoRuta { get; set; } = string.Empty;
        public decimal TiempoCiclo { get; set; }
        public string NumeroParte { get; set; } = string.Empty;
        public string Estatus { get; set; } = string.Empty;
    }
}

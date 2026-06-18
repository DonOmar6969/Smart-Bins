namespace SmartBins.Modelos
{
    // Cada propiedad corresponde a una columna en la tabla de la base de datos
    public class Componente
    {
        public int ComponenteID { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string FotoRuta { get; set; }
    }
}

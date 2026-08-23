namespace SmartBins.Preparacion.Modelos;

public sealed class EnsambleOpcion
{
    public string Nombre { get; init; } = string.Empty;
    public string NumeroParte { get; init; } = string.Empty;
    public string UnidadTrabajo { get; init; } = string.Empty;
    public int NumeroEstaciones { get; init; }
    public string Descripcion => string.IsNullOrWhiteSpace(NumeroParte)
        ? Nombre
        : $"{Nombre}  ·  {NumeroParte}";
}

public sealed class ElementoPreparacion
{
    public int Estacion { get; init; }
    public int Orden { get; init; }
    public string Posicion { get; init; } = string.Empty;
    public string Bin { get; init; } = string.Empty;
    public string Componente { get; init; } = string.Empty;
    public string NumeroParte { get; init; } = string.Empty;
    public string FotoRuta { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public bool Confirmado { get; set; }
    public string Titulo => $"{Posicion}  ·  {Bin}";
    public string Detalle => $"{Componente}  ·  PN {NumeroParte}";
    public string CantidadTexto => $"Cantidad: {Cantidad}";
}

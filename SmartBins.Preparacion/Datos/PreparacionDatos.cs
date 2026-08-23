using System.Configuration;
using Microsoft.Data.SqlClient;
using SmartBins.Preparacion.Modelos;

namespace SmartBins.Preparacion.Datos;

public sealed class PreparacionDatos
{
    private readonly string cadena = ConfigurationManager.ConnectionStrings["SmartBinsConnection"]?.ConnectionString
        ?? "Server=(localdb)\\MSSQLLocalDB;Database=SmartBins;Integrated Security=True;TrustServerCertificate=True;";

    public async Task<List<EnsambleOpcion>> ObtenerEnsamblesAsync()
    {
        const string sql = """
            SELECT e.Nombre, e.NumeroParte, e.UnidadTrabajo,
                   ISNULL(MAX(s.EstacionAsignada), 0) AS NumeroEstaciones
            FROM SB.Ensambles e
            LEFT JOIN SB.SecuenciaEnsamble s ON s.EnsambleNombre = e.Nombre
            GROUP BY e.Nombre, e.NumeroParte, e.UnidadTrabajo
            ORDER BY e.Nombre;
            """;

        var resultado = new List<EnsambleOpcion>();
        await using var conexion = new SqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            resultado.Add(new EnsambleOpcion
            {
                Nombre = lector["Nombre"]?.ToString() ?? string.Empty,
                NumeroParte = lector["NumeroParte"] == DBNull.Value ? string.Empty : lector["NumeroParte"].ToString() ?? string.Empty,
                UnidadTrabajo = lector["UnidadTrabajo"] == DBNull.Value ? string.Empty : lector["UnidadTrabajo"].ToString() ?? string.Empty,
                NumeroEstaciones = Convert.ToInt32(lector["NumeroEstaciones"])
            });
        }
        return resultado;
    }

    public async Task<List<ElementoPreparacion>> CrearPlanAsync(string ensamble)
    {
        var bins = await ObtenerBinsAsync();
        const string sql = """
            SELECT s.EstacionAsignada, MIN(s.OrdenSecuencia) AS Orden,
                   s.ComponenteID, s.NombreComponente,
                   COALESCE(c.NumeroParte, '') AS NumeroParte,
                   COALESCE(c.FotoRuta, '') AS FotoRuta,
                   COUNT(*) AS Cantidad
            FROM SB.SecuenciaEnsamble s
            LEFT JOIN SB.Componentes c ON c.ComponenteID = s.ComponenteID
            WHERE s.EnsambleNombre = @Ensamble
            GROUP BY s.EstacionAsignada, s.ComponenteID, s.NombreComponente,
                     c.NumeroParte, c.FotoRuta
            ORDER BY s.EstacionAsignada, MIN(s.OrdenSecuencia);
            """;

        var resultado = new List<ElementoPreparacion>();
        var indicesPorEstacion = new Dictionary<int, int>();
        await using var conexion = new SqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@Ensamble", ensamble);
        await using var lector = await comando.ExecuteReaderAsync();
        int indiceBin = 0;
        while (await lector.ReadAsync())
        {
            int estacion = Math.Max(1, Convert.ToInt32(lector["EstacionAsignada"]));
            int posicion = indicesPorEstacion.GetValueOrDefault(estacion) + 1;
            indicesPorEstacion[estacion] = posicion;
            resultado.Add(new ElementoPreparacion
            {
                Estacion = estacion,
                Orden = Convert.ToInt32(lector["Orden"]),
                Posicion = CrearPosicion(posicion),
                Bin = indiceBin < bins.Count ? bins[indiceBin++] : "BIN PENDIENTE",
                Componente = lector["NombreComponente"]?.ToString() ?? "Componente",
                NumeroParte = lector["NumeroParte"]?.ToString() ?? string.Empty,
                FotoRuta = lector["FotoRuta"]?.ToString() ?? string.Empty,
                Cantidad = Convert.ToInt32(lector["Cantidad"])
            });
        }
        return resultado;
    }

    private async Task<List<string>> ObtenerBinsAsync()
    {
        var resultado = new List<string>();
        await using var conexion = new SqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand("SELECT Codigo FROM SB.Bins ORDER BY Codigo", conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            resultado.Add(lector["Codigo"]?.ToString() ?? string.Empty);
        return resultado;
    }

    private static string CrearPosicion(int indice)
    {
        int baseCero = indice - 1;
        char fila = (char)('A' + baseCero / 4);
        int columna = baseCero % 4 + 1;
        return $"{fila}{columna}";
    }
}

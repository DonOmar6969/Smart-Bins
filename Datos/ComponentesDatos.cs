using Microsoft.Data.SqlClient;
using SmartBins.Modelos;

namespace SmartBins.Datos
{
    public class ComponentesDatos
    {
        private readonly Conexion conexion = new Conexion();

        public List<Componente> ObtenerTodos()
        {
            var lista = new List<Componente>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT ComponenteID, Nombre, Descripcion, FotoRuta, NumeroParte, TiempoCiclo, Estatus FROM SB.Componentes";
                SqlDataReader reader = new SqlCommand(query, con).ExecuteReader();
                while (reader.Read())
                    lista.Add(MapearComponente(reader));
            }
            return lista;
        }

        public bool Agregar(Componente c)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = @"
                    IF EXISTS (
                        SELECT 1
                        FROM SB.Componentes WITH (UPDLOCK, HOLDLOCK)
                        WHERE UPPER(LTRIM(RTRIM(NumeroParte))) =
                              UPPER(LTRIM(RTRIM(@NumeroParte)))
                    )
                        SELECT CAST(0 AS bit);
                    ELSE
                    BEGIN
                        INSERT INTO SB.Componentes
                            (Nombre, Descripcion, FotoRuta, NumeroParte, TiempoCiclo, Estatus)
                        VALUES
                            (@Nombre, @Descripcion, @FotoRuta, @NumeroParte, @TiempoCiclo, @Estatus);
                        SELECT CAST(1 AS bit);
                    END";
                var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", c.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", c.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FotoRuta", c.FotoRuta ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroParte", c.NumeroParte?.Trim() ?? string.Empty);
                cmd.Parameters.AddWithValue("@TiempoCiclo", c.TiempoCiclo);
                cmd.Parameters.AddWithValue("@Estatus", c.Estatus ?? "Desactualizado");
                return Convert.ToBoolean(cmd.ExecuteScalar());
            }
        }

        public bool ExisteNumeroParte(string numeroParte, int componenteIdExcluir = 0)
        {
            if (string.IsNullOrWhiteSpace(numeroParte)) return false;

            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = @"
                    SELECT COUNT(*)
                    FROM SB.Componentes
                    WHERE UPPER(LTRIM(RTRIM(NumeroParte))) =
                          UPPER(LTRIM(RTRIM(@NumeroParte)))
                      AND (@ComponenteIDExcluir = 0 OR ComponenteID <> @ComponenteIDExcluir)";
                var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@NumeroParte", numeroParte.Trim());
                cmd.Parameters.AddWithValue("@ComponenteIDExcluir", componenteIdExcluir);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public void Actualizar(Componente c)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = @"UPDATE SB.Componentes
                                 SET Nombre      = @Nombre,
                                     Descripcion = @Descripcion,
                                     FotoRuta    = @FotoRuta,
                                     NumeroParte = @NumeroParte,
                                     TiempoCiclo = @TiempoCiclo,
                                     Estatus     = @Estatus
                                 WHERE ComponenteID = @ComponenteID";
                var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ComponenteID", c.ComponenteID);
                cmd.Parameters.AddWithValue("@Nombre", c.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", c.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FotoRuta", c.FotoRuta ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroParte", c.NumeroParte ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TiempoCiclo", c.TiempoCiclo);
                cmd.Parameters.AddWithValue("@Estatus", c.Estatus ?? "Desactualizado");
                cmd.ExecuteNonQuery();
            }
        }

        public void Eliminar(int componenteID)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                var cmd = new SqlCommand("DELETE FROM SB.Componentes WHERE ComponenteID = @ComponenteID", con);
                cmd.Parameters.AddWithValue("@ComponenteID", componenteID);
                cmd.ExecuteNonQuery();
            }
        }

        public List<string> ObtenerTipos()
        {
            var tipos = new List<string>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                var reader = new SqlCommand("SELECT DISTINCT Nombre FROM SB.Componentes ORDER BY Nombre", con).ExecuteReader();
                while (reader.Read())
                    tipos.Add(reader["Nombre"].ToString());
            }
            return tipos;
        }

        public List<Componente> ObtenerPorFiltro(string tipo, string numeroParte)
        {
            var lista = new List<Componente>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT ComponenteID, Nombre, Descripcion, NumeroParte, TiempoCiclo, Estatus FROM SB.Componentes WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(tipo))
                    query += " AND Nombre = @Nombre";
                if (!string.IsNullOrWhiteSpace(numeroParte))
                    query += " AND NumeroParte LIKE @NumeroParte";

                var cmd = new SqlCommand(query, con);
                if (!string.IsNullOrWhiteSpace(tipo))
                    cmd.Parameters.AddWithValue("@Nombre", tipo);
                if (!string.IsNullOrWhiteSpace(numeroParte))
                    cmd.Parameters.AddWithValue("@NumeroParte", "%" + numeroParte + "%");

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new Componente
                    {
                        ComponenteID = reader["ComponenteID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ComponenteID"]),
                        Nombre = reader["Nombre"] == DBNull.Value ? "" : reader["Nombre"].ToString(),
                        NumeroParte = reader["NumeroParte"] == DBNull.Value ? "" : reader["NumeroParte"].ToString(),
                        Descripcion = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString(),
                        TiempoCiclo = reader["TiempoCiclo"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TiempoCiclo"]),
                        Estatus = reader["Estatus"] == DBNull.Value ? "Desactualizado" : reader["Estatus"].ToString()
                    });
            }
            return lista;
        }

        public Componente ObtenerPorNumeroParte(string numeroParte)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string sql = @"SELECT TOP 1 ComponenteID, NumeroParte, Nombre, Descripcion, FotoRuta, TiempoCiclo, Estatus
                               FROM SB.Componentes
                               WHERE LTRIM(RTRIM(NumeroParte)) = LTRIM(RTRIM(@NumeroParte))";
                var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@NumeroParte", numeroParte?.Trim() ?? string.Empty);
                var reader = cmd.ExecuteReader();
                return reader.Read() ? MapearComponente(reader) : null;
            }
        }

        // Mapeo centralizado para no repetir lógica
        private Componente MapearComponente(SqlDataReader r) => new Componente
        {
            ComponenteID = r["ComponenteID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ComponenteID"]),
            NumeroParte = r["NumeroParte"] == DBNull.Value ? "" : r["NumeroParte"].ToString(),
            Nombre = r["Nombre"] == DBNull.Value ? "" : r["Nombre"].ToString(),
            Descripcion = r["Descripcion"] == DBNull.Value ? "" : r["Descripcion"].ToString(),
            FotoRuta = r["FotoRuta"] == DBNull.Value ? "" : r["FotoRuta"].ToString(),
            TiempoCiclo = r["TiempoCiclo"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TiempoCiclo"]),
            Estatus = r["Estatus"] == DBNull.Value ? "Desactualizado" : r["Estatus"].ToString()
        };
    }
}

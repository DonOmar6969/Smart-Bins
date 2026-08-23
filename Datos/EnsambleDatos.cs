using Microsoft.Data.SqlClient;
using SmartBins.Modelos;
using System.Windows;


namespace SmartBins.Datos
{
    public class EnsambleDatos
    {
        private readonly Conexion conexion = new Conexion();
        public List<Ensamble> ObtenerTodos()
        {
            List<Ensamble> lista = new List<Ensamble>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT Nombre, TiempoEstimado, UnidadTrabajo, NumeroOperadoresDefault FROM SB.Ensambles";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Ensamble
                    {
                        Nombre = reader["Nombre"].ToString(),
                        TiempoEstimado = reader["TiempoEstimado"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TiempoEstimado"]),
                        UnidadTrabajo = reader["UnidadTrabajo"].ToString(),
                        NumeroOperadoresDefault = reader["NumeroOperadoresDefault"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NumeroOperadoresDefault"])
                    });
                }
            }
            lista.ForEach(Servicios.ControlEnsambleService.Aplicar);
            return lista;
        }

        public void Agregar(Ensamble ensamble)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "INSERT INTO SB.Ensambles (Nombre, NumeroParte, TiempoEstimado, UnidadTrabajo, NumeroOperadoresDefault) VALUES (@Nombre, @NumeroParte, @TiempoEstimado, @UnidadTrabajo, @NumeroOperadoresDefault)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", ensamble.Nombre);
                cmd.Parameters.AddWithValue("@NumeroParte", ensamble.NumeroParte ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TiempoEstimado", ensamble.TiempoEstimado == 0 ? (object)DBNull.Value : ensamble.TiempoEstimado);
                cmd.Parameters.AddWithValue("@UnidadTrabajo", ensamble.UnidadTrabajo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroOperadoresDefault", ensamble.NumeroOperadoresDefault == 0 ? (object)DBNull.Value : ensamble.NumeroOperadoresDefault);
                cmd.ExecuteNonQuery();
            }
        }
        public List<Ensamble> ObtenerPorFiltro(string nombre, string unidadTrabajo)
        {
            List<Ensamble> lista = new List<Ensamble>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT Nombre, TiempoEstimado, UnidadTrabajo, NumeroOperadoresDefault FROM SB.Ensambles WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(nombre))
                    query += " AND Nombre LIKE @Nombre";
                if (!string.IsNullOrWhiteSpace(unidadTrabajo))
                    query += " AND UnidadTrabajo = @UnidadTrabajo";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrWhiteSpace(nombre))
                    cmd.Parameters.AddWithValue("@Nombre", "%" + nombre + "%");
                if (!string.IsNullOrWhiteSpace(unidadTrabajo))
                    cmd.Parameters.AddWithValue("@UnidadTrabajo", unidadTrabajo);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Ensamble
                    {
                        Nombre = reader["Nombre"].ToString(),
                        TiempoEstimado = reader["TiempoEstimado"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TiempoEstimado"]),
                        UnidadTrabajo = reader["UnidadTrabajo"].ToString(),
                        NumeroOperadoresDefault = reader["NumeroOperadoresDefault"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NumeroOperadoresDefault"])
                    });
                }
            }
            lista.ForEach(Servicios.ControlEnsambleService.Aplicar);
            return lista;
        }
        public void Actualizar(Ensamble ensamble)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "UPDATE SB.Ensambles SET TiempoEstimado = @TiempoEstimado, UnidadTrabajo = @UnidadTrabajo, NumeroOperadoresDefault = @NumeroOperadoresDefault WHERE Nombre = @Nombre";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", ensamble.Nombre);
                cmd.Parameters.AddWithValue("@TiempoEstimado", ensamble.TiempoEstimado);
                cmd.Parameters.AddWithValue("@UnidadTrabajo", ensamble.UnidadTrabajo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroOperadoresDefault", ensamble.NumeroOperadoresDefault);
                cmd.ExecuteNonQuery();
            }
        }
        public bool Existe(string nombre)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM SB.Ensambles WHERE Nombre = @Nombre";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
        public void Eliminar(string nombre)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "DELETE FROM SB.Ensambles WHERE Nombre = @Nombre";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

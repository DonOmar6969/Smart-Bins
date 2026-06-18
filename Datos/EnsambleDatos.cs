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
                string query = "SELECT Nombre, TiempoEstimado, UnidadTrabajo, NumeroOperadoresDefault FROM Ensambles";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Ensamble
                    {
                        Nombre = reader["Nombre"].ToString(),
                        TiempoEstimado = reader["TiempoEstimado"] == DBNull.Value ? 0 : (int)reader["TiempoEstimado"],
                        UnidadTrabajo = reader["UnidadTrabajo"].ToString(),
                        NumeroOperadoresDefault = reader["NumeroOperadoresDefault"] == DBNull.Value ? 0 : (int)reader["NumeroOperadoresDefault"]
                    });
                }
            }
            return lista;
        }

        public void Agregar(Ensamble ensamble)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "INSERT INTO Ensambles (Nombre, TiempoEstimado, UnidadTrabajo, NumeroOperadoresDefault) VALUES (@Nombre, @TiempoEstimado, @UnidadTrabajo, @NumeroOperadoresDefault)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", ensamble.Nombre);
                cmd.Parameters.AddWithValue("@TiempoEstimado", ensamble.TiempoEstimado == 0 ? (object)DBNull.Value : ensamble.TiempoEstimado);
                cmd.Parameters.AddWithValue("@UnidadTrabajo", ensamble.UnidadTrabajo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NumeroOperadoresDefault", ensamble.NumeroOperadoresDefault == 0 ? (object)DBNull.Value : ensamble.NumeroOperadoresDefault);
                try 
                {
                    cmd.ExecuteNonQuery();
                } 
                catch 
                {
                    MessageBox.Show("Error al agregar el ensamble. Verifique que el nombre no esté duplicado.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public List<Ensamble> ObtenerPorFiltro(string nombre, string unidadTrabajo)
        {
            List<Ensamble> lista = new List<Ensamble>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT Nombre, TiempoEstimado, UnidadTrabajo, NumeroOperadoresDefault FROM Ensambles WHERE 1=1";

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
                        TiempoEstimado = reader["TiempoEstimado"] == DBNull.Value ? 0 : (int)reader["TiempoEstimado"],
                        UnidadTrabajo = reader["UnidadTrabajo"].ToString(),
                        NumeroOperadoresDefault = reader["NumeroOperadoresDefault"] == DBNull.Value ? 0 : (int)reader["NumeroOperadoresDefault"]
                    });
                }
            }
            return lista;
        }
        public void Actualizar(Ensamble ensamble)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "UPDATE Ensambles SET TiempoEstimado = @TiempoEstimado, UnidadTrabajo = @UnidadTrabajo, NumeroOperadoresDefault = @NumeroOperadoresDefault WHERE Nombre = @Nombre";
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
                string query = "SELECT COUNT(*) FROM Ensambles WHERE Nombre = @Nombre";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        public void Eliminar(string nombre)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "DELETE FROM Ensambles WHERE Nombre = @Nombre";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
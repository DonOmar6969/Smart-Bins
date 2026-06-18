using Microsoft.Data.SqlClient;
using SmartBins.Modelos;

namespace SmartBins.Datos
{
    public class ComponentesDatos
    {
        private readonly Conexion conexion = new Conexion();

        public List<Componente> ObtenerTodos()
        {
            List<Componente> lista = new List<Componente>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT ComponenteID, Nombre, Descripcion, FotoRuta FROM Componentes";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Componente
                    {
                        ComponenteID = (int)reader["ComponenteID"],
                        Nombre = reader["Nombre"].ToString(),
                        Descripcion = reader["Descripcion"].ToString(),
                        FotoRuta = reader["FotoRuta"].ToString()
                    });
                }
            }
            return lista;
        }

        public void Agregar(Componente componente)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "INSERT INTO Componentes (Nombre, Descripcion, FotoRuta) VALUES (@Nombre, @Descripcion, @FotoRuta)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombre", componente.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", componente.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FotoRuta", componente.FotoRuta ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void Actualizar(Componente componente)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "UPDATE Componentes SET Nombre = @Nombre, Descripcion = @Descripcion, FotoRuta = @FotoRuta WHERE ComponenteID = @ComponenteID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ComponenteID", componente.ComponenteID);
                cmd.Parameters.AddWithValue("@Nombre", componente.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", componente.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FotoRuta", componente.FotoRuta ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void Eliminar(int componenteID)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "DELETE FROM Componentes WHERE ComponenteID = @ComponenteID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ComponenteID", componenteID);
                cmd.ExecuteNonQuery();
            }
        }

        public List<string> ObtenerTipos()
        {
            List<string> tipos = new List<string>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT DISTINCT Nombre FROM Componentes ORDER BY Nombre";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    tipos.Add(reader["Nombre"].ToString());
            }
            return tipos;
        }

        public List<Componente> ObtenerPorFiltro(string tipo, string especificacion)
        {
            List<Componente> lista = new List<Componente>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT ComponenteID, Nombre, Descripcion FROM Componentes WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(tipo))
                    query += " AND Nombre = @Nombre";
                if (!string.IsNullOrWhiteSpace(especificacion))
                    query += " AND Descripcion LIKE @Descripcion";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrWhiteSpace(tipo))
                    cmd.Parameters.AddWithValue("@Nombre", tipo);
                if (!string.IsNullOrWhiteSpace(especificacion))
                    cmd.Parameters.AddWithValue("@Descripcion", "%" + especificacion + "%");

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Componente
                    {
                        ComponenteID = (int)reader["ComponenteID"],
                        Nombre = reader["Nombre"].ToString(),
                        Descripcion = reader["Descripcion"].ToString()
                    });
                }
            }
            return lista;
        }
    }
}
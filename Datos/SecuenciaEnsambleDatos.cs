using Microsoft.Data.SqlClient;
using SmartBins.Modelos;
namespace SmartBins.Datos
{
    public class SecuenciaEnsambleDatos
    {
        private readonly Conexion conexion = new Conexion();

        public void AgregarLista(List<SecuenciaEnsamble> lista)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                foreach (var item in lista)
                {
                    string query = "INSERT INTO SecuenciaEnsamble (EnsambleNombre, ComponenteID, OrdenSecuencia, TiempoEstimado, NombreComponente, EspecificacionComponente, EstacionAsignada) VALUES (@EnsambleNombre, @ComponenteID, @OrdenSecuencia, @TiempoEstimado, @NombreComponente, @EspecificacionComponente, @EstacionAsignada)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@EnsambleNombre", item.EnsambleNombre);
                    cmd.Parameters.AddWithValue("@ComponenteID", item.ComponenteID);
                    cmd.Parameters.AddWithValue("@OrdenSecuencia", item.OrdenSecuencia);
                    cmd.Parameters.AddWithValue("@TiempoEstimado", item.TiempoEstimado);
                    cmd.Parameters.AddWithValue("@NombreComponente", item.NombreComponente);
                    cmd.Parameters.AddWithValue("@EspecificacionComponente", item.EspecificacionComponente);
                    cmd.Parameters.AddWithValue("@EstacionAsignada", item.EstacionAsignada);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<SecuenciaEnsamble> ObtenerPorEnsamble(string ensambleNombre)
        {
            List<SecuenciaEnsamble> lista = new List<SecuenciaEnsamble>();
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "SELECT SecuenciaID, EnsambleNombre, ComponenteID, OrdenSecuencia, TiempoEstimado, NombreComponente, EspecificacionComponente, EstacionAsignada FROM SecuenciaEnsamble WHERE EnsambleNombre = @EnsambleNombre ORDER BY OrdenSecuencia";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@EnsambleNombre", ensambleNombre);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new SecuenciaEnsamble
                    {
                        SecuenciaID = (int)reader["SecuenciaID"],
                        EnsambleNombre = reader["EnsambleNombre"].ToString(),
                        ComponenteID = (int)reader["ComponenteID"],
                        OrdenSecuencia = (int)reader["OrdenSecuencia"],
                        TiempoEstimado = (decimal)reader["TiempoEstimado"],
                        NombreComponente = reader["NombreComponente"].ToString(),
                        EspecificacionComponente = reader["EspecificacionComponente"].ToString(),
                        EstacionAsignada = (int)reader["EstacionAsignada"]
                    });
                }
            }
            return lista;
        }

        public void EliminarPorEnsamble(string ensambleNombre)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "DELETE FROM SecuenciaEnsamble WHERE EnsambleNombre = @EnsambleNombre";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@EnsambleNombre", ensambleNombre);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
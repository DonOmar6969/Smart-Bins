using Microsoft.Data.SqlClient;
using SmartBins.Modelos;
using System.Windows;

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
                    string query =  @"INSERT INTO SB.SecuenciaEnsamble 
                                    (EnsambleNombre, ComponenteID, 
                                    OrdenSecuencia, TiempoEstimado,
                                    NombreComponente,
                                    EspecificacionComponente,
                                    EstacionAsignada,
                                    Referencia, 
                                    OrdenPrecedente,
                                    ImagenRuta) 
                                    VALUES 
                                    (@EnsambleNombre,
                                    @ComponenteID, 
                                    @OrdenSecuencia,
                                    @TiempoEstimado,
                                    @NombreComponente,
                                    @EspecificacionComponente,
                                    @EstacionAsignada,
                                    @Referencia,
                                    @OrdenPrecedente,
                                    @ImagenRuta)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@ImagenRuta", string.IsNullOrWhiteSpace(item.ImagenRuta) ? (object)DBNull.Value : item.ImagenRuta);
                    cmd.Parameters.AddWithValue("@EnsambleNombre", item.EnsambleNombre);
                    cmd.Parameters.AddWithValue("@ComponenteID", item.ComponenteID);
                    cmd.Parameters.AddWithValue("@OrdenSecuencia", item.OrdenSecuencia);
                    cmd.Parameters.AddWithValue("@TiempoEstimado", item.TiempoEstimado);
                    cmd.Parameters.AddWithValue("@NombreComponente", item.NombreComponente);
                    cmd.Parameters.AddWithValue("@EspecificacionComponente", item.EspecificacionComponente);
                    cmd.Parameters.AddWithValue("@EstacionAsignada", item.EstacionAsignada);
                    cmd.Parameters.AddWithValue("@Referencia", string.IsNullOrWhiteSpace(item.Referencia) ? (object)DBNull.Value : item.Referencia);
                    cmd.Parameters.AddWithValue("@OrdenPrecedente", item.OrdenPrecedente == 0 ? (object)DBNull.Value : item.OrdenPrecedente);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<SecuenciaEnsamble> ObtenerPorEnsamble(string ensambleNombre)
        {
            List<SecuenciaEnsamble> lista = new List<SecuenciaEnsamble>();
            try
            {
                using (SqlConnection con = conexion.ObtenerConexion())
                {
                    con.Open();
                    string query = "SELECT SecuenciaID, EnsambleNombre, ComponenteID, OrdenSecuencia, TiempoEstimado, NombreComponente, EspecificacionComponente, EstacionAsignada, Referencia, OrdenPrecedente, ImagenRuta FROM SB.SecuenciaEnsamble WHERE EnsambleNombre = @EnsambleNombre ORDER BY OrdenSecuencia";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@EnsambleNombre", ensambleNombre);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        lista.Add(new SecuenciaEnsamble
                        {
                            SecuenciaID = Convert.ToInt32(reader["SecuenciaID"]),
                            EnsambleNombre = reader["EnsambleNombre"].ToString(),
                            ComponenteID = Convert.ToInt32(reader["ComponenteID"]),
                            OrdenSecuencia = Convert.ToInt32(reader["OrdenSecuencia"]),
                            TiempoEstimado = (decimal)reader["TiempoEstimado"],
                            NombreComponente = reader["NombreComponente"].ToString(),
                            EspecificacionComponente = reader["EspecificacionComponente"].ToString(),
                            EstacionAsignada = Convert.ToInt32(reader["EstacionAsignada"]),
                            Referencia = reader["Referencia"] == DBNull.Value ? null : reader["Referencia"].ToString(),
                            OrdenPrecedente = reader["OrdenPrecedente"] == DBNull.Value ? 0 : Convert.ToInt32(reader["OrdenPrecedente"]),
                            ImagenRuta = reader["ImagenRuta"] == DBNull.Value ? null : reader["ImagenRuta"].ToString()
                        });

                    }
                }
            }
            catch //                 
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Error: al leer la secuencia de ensamble. Verifique que los datos en la base de datos sean correctos.", "Error de secuencia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return lista;
        }

        public void EliminarPorEnsamble(string ensambleNombre)
        {
            using (SqlConnection con = conexion.ObtenerConexion())
            {
                con.Open();
                string query = "DELETE FROM SB.SecuenciaEnsamble WHERE EnsambleNombre = @EnsambleNombre";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@EnsambleNombre", ensambleNombre);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

using Microsoft.Data.SqlClient;
using SmartBins.Modelos;
using System.Windows;
namespace SmartBins.Datos
{
    public class BinDatos
    {
        private readonly Conexion conexion = new Conexion();

        public List<Bin> ObtenerTodos() // Retorna una lista de todos los bins en la base de datos
        {
            List<Bin> lista = new List<Bin>();
            try
            {
                using (SqlConnection con = conexion.ObtenerConexion())
                {
                    con.Open();
                    string query = "SELECT Codigo, Descripcion FROM SB.Bins";
                    SqlCommand cmd = new SqlCommand(query, con);            // Crea un comando SQL para seleccionar el código y la descripción de todos los bins
                    SqlDataReader reader = cmd.ExecuteReader();             // Ejecuta la consulta y obtiene un lector de datos
                    while (reader.Read())                                   // Itera a través de los resultados y agrega cada bin a la lista (Se agregan objetos de la clase bin)
                    {
                        lista.Add(new Bin                                   // La lista va a estar llena de objetos de tipo Bin, cada uno con su código y descripción
                        {
                            Codigo = reader["Codigo"].ToString(),
                            Descripcion = reader["Descripcion"].ToString() // Codigo y descripción son parametros de objeto bin
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error al obtener los bins: {ex.Message}", "Error de consulta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return lista;
        }
        public void Agregar(Bin bin)                                    // Agrega un nuevo bin a la base de datos
        {
            try
            {
                using (SqlConnection con = conexion.ObtenerConexion())
                {
                    con.Open();
                    string query = "INSERT INTO SB.Bins (Codigo, Descripcion) VALUES (@Codigo, @Descripcion)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Codigo", bin.Codigo);
                    cmd.Parameters.AddWithValue("@Descripcion", bin.Descripcion);
                    cmd.ExecuteNonQuery();
                    global::SmartBins.Servicios.DialogoSmart.Mostrar("Bin agregado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar("Error: El código del bin ya existe. Por favor, ingrese un código único.", "Error de duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Actualizar(Bin bin)
        {
            try
            {
                using (SqlConnection con = conexion.ObtenerConexion())
                {
                    con.Open();
                    string query = "UPDATE SB.Bins SET Descripcion = @Descripcion WHERE Codigo = @Codigo";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Codigo", bin.Codigo);
                    cmd.Parameters.AddWithValue("@Descripcion", bin.Descripcion);
                    cmd.ExecuteNonQuery();
                    global::SmartBins.Servicios.DialogoSmart.Mostrar("Bin actualizado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (SqlException ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error al actualizar el bin: {ex.Message}", "Error de actualización", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Eliminar(string codigo)
        {
            try
            {
                using (SqlConnection con = conexion.ObtenerConexion())
                {
                    con.Open();
                    string query = "DELETE FROM SB.Bins WHERE Codigo = @Codigo";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.ExecuteNonQuery();
                    global::SmartBins.Servicios.DialogoSmart.Mostrar("Bin eliminado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (SqlException ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error al eliminar el bin: {ex.Message}", "Error de eliminación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                global::SmartBins.Servicios.DialogoSmart.Mostrar($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

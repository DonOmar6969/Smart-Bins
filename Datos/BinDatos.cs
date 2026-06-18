using Microsoft.Data.SqlClient;
using SmartBins.Modelos;
using System.Windows;
namespace SmartBins.Datos
{
    internal class BinDatos
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
                    string query = "SELECT Codigo, Descripcion FROM Bins";
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
                MessageBox.Show($"Error al obtener los bins: {ex.Message}", "Error de consulta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    string query = "INSERT INTO Bins (Codigo, Descripcion) VALUES (@Codigo, @Descripcion)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Codigo", bin.Codigo);
                    cmd.Parameters.AddWithValue("@Descripcion", bin.Descripcion);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Bin agregado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("Error: El código del bin ya existe. Por favor, ingrese un código único.", "Error de duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Actualizar(Bin bin)
        {
            try
            {
                using (SqlConnection con = conexion.ObtenerConexion())
                {
                    con.Open();
                    string query = "UPDATE Bins SET Descripcion = @Descripcion WHERE Codigo = @Codigo";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Codigo", bin.Codigo);
                    cmd.Parameters.AddWithValue("@Descripcion", bin.Descripcion);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Bin actualizado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error al actualizar el bin: {ex.Message}", "Error de actualización", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Eliminar(string codigo)
        {
            try
            {
                using (SqlConnection con = conexion.ObtenerConexion())
                {
                    con.Open();
                    string query = "DELETE FROM Bins WHERE Codigo = @Codigo";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Bin eliminado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error al eliminar el bin: {ex.Message}", "Error de eliminación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

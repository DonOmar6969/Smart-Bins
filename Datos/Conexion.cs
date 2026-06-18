using Microsoft.Data.SqlClient;
using System.Configuration;

namespace SmartBins.Datos
{
    internal class Conexion
    {
        private readonly string cadenaConexion;

        public Conexion()
        {
            cadenaConexion = ConfigurationManager.ConnectionStrings["SmartBinsConnection"]?.ConnectionString
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=SmartBins;Integrated Security=True;";
        }

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(cadenaConexion);
        }
    }
}

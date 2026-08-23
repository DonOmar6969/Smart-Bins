using SmartBins.Modelos;
using System.IO;

namespace SmartBins.Datos
{
    public static class ImagenesArchivosService
    {
        public static int EliminarImagenesDeSecuencia(IEnumerable<SecuenciaEnsamble> pasos)
        {
            var rutas = pasos
                .Where(p => !string.IsNullOrWhiteSpace(p.ImagenRuta))
                .SelectMany(p => p.ImagenRuta.Split(
                    ';', StringSplitOptions.RemoveEmptyEntries));

            return EliminarRutasAdministradas(rutas, "Secuencia");
        }

        public static int EliminarImagenesNuevasDeBorrador(IEnumerable<string> rutas)
            => EliminarRutasAdministradas(rutas, "Secuencia");

        public static int EliminarFotoDeComponente(string ruta)
            => EliminarRutasAdministradas(new[] { ruta }, "Componentes");

        private static int EliminarRutasAdministradas(
            IEnumerable<string> rutas, string subcarpetaPermitida)
        {
            string escritorio = Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop);
            string carpetaPermitida = Path.GetFullPath(Path.Combine(
                escritorio, "Smart_Bins_Imagenes", subcarpetaPermitida));
            string prefijoPermitido = carpetaPermitida.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            int eliminadas = 0;
            foreach (string ruta in rutas.Where(r => !string.IsNullOrWhiteSpace(r))
                                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    string rutaCompleta = Path.GetFullPath(ruta);
                    if (!rutaCompleta.StartsWith(
                            prefijoPermitido, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (File.Exists(rutaCompleta))
                    {
                        File.Delete(rutaCompleta);
                        eliminadas++;
                    }
                }
                catch
                {
                    // Una imagen bloqueada no debe impedir eliminar el registro.
                }
            }

            return eliminadas;
        }
    }
}

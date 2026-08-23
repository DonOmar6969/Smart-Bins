using System.IO;
using System.Text.Json;
using SmartBins.Modelos;
using SmartBins.Modelos.Produccion;

namespace SmartBins.Servicios
{
    public static class ConfiguracionAplicacionService
    {
        private static readonly string Carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartBins");
        private static readonly string Ruta = Path.Combine(Carpeta, "configuracion.json");

        public static ConfiguracionAplicacion Cargar()
        {
            try
            {
                if (!File.Exists(Ruta)) return new ConfiguracionAplicacion();
                var configuracion = JsonSerializer.Deserialize<ConfiguracionAplicacion>(File.ReadAllText(Ruta))
                    ?? new ConfiguracionAplicacion();
                Normalizar(configuracion);
                return configuracion;
            }
            catch
            {
                return new ConfiguracionAplicacion();
            }
        }

        public static void Guardar(ConfiguracionAplicacion configuracion)
        {
            Validar(configuracion);
            Normalizar(configuracion);
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Ruta, JsonSerializer.Serialize(configuracion,
                new JsonSerializerOptions { WriteIndented = true }));
        }

        public static ConfiguracionBalanceo CrearConfiguracionBalanceo()
        {
            var c = Cargar();
            return new ConfiguracionBalanceo
            {
                UmbralRepeticionesParaDistribuir = c.UmbralRepeticionesParaDistribuir,
                ProporcionMezclaAlta = c.ProporcionMezclaAltaPorcentaje / 100m,
                MinimoPartesDistintasParaMezclaAlta = c.MinimoPartesDistintasMezclaAlta,
                LongitudSufijoCorto = c.LongitudSufijoExacto,
                LongitudSufijoLargo = c.LongitudSufijoParecido,
                DiferenciasPermitidasSufijoLargo = c.DiferenciasPermitidasSufijo
            };
        }

        public static void Restablecer()
        {
            Guardar(new ConfiguracionAplicacion());
        }

        private static void Validar(ConfiguracionAplicacion c)
        {
            if (c.OperadoresPredeterminados < 1 || c.OperadoresPredeterminados > 50)
                throw new InvalidOperationException("Los operadores predeterminados deben estar entre 1 y 50.");
            if (c.UnidadesPruebaPredeterminadas < 1 || c.UnidadesPruebaPredeterminadas > 100000)
                throw new InvalidOperationException("Las unidades de prueba deben estar entre 1 y 100,000.");
            if (c.NivelesTiempoCicloSegundos.Count == 0 ||
                c.NivelesTiempoCicloSegundos.Any(x => x is < 1 or > 30))
                throw new InvalidOperationException("Cada nivel de tiempo debe estar entre 1 y 30 segundos.");
            if (c.UmbralRepeticionesParaDistribuir < 2)
                throw new InvalidOperationException("El umbral de repetición debe ser al menos 2.");
            if (c.ProporcionMezclaAltaPorcentaje < 1 || c.ProporcionMezclaAltaPorcentaje > 100)
                throw new InvalidOperationException("El porcentaje de mezcla alta debe estar entre 1 y 100.");
            if (c.MinimoPartesDistintasMezclaAlta < 1)
                throw new InvalidOperationException("La cantidad mínima de partes distintas debe ser mayor que cero.");
            if (c.LongitudSufijoExacto < 1 || c.LongitudSufijoParecido < 1 || c.DiferenciasPermitidasSufijo < 0)
                throw new InvalidOperationException("Los parámetros de similitud no son válidos.");
        }

        private static void Normalizar(ConfiguracionAplicacion c)
        {
            c.NivelesTiempoCicloSegundos = c.NivelesTiempoCicloSegundos
                .Where(x => x is >= 1 and <= 30).Distinct().OrderBy(x => x).ToList();
            if (c.NivelesTiempoCicloSegundos.Count == 0)
                c.NivelesTiempoCicloSegundos = new() { 2, 4, 6, 8, 10 };
        }
    }
}

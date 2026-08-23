using SmartBins.Modelos;
using System.IO;

namespace SmartBins.Servicios
{
    public sealed class ResultadoValidacionEnsamble
    {
        public List<string> Errores { get; } = new();
        public List<string> Advertencias { get; } = new();
        public bool EsValido => Errores.Count == 0;

        public string CrearResumen()
        {
            var lineas = new List<string>();
            if (Errores.Count > 0)
            {
                lineas.Add("Datos obligatorios:");
                lineas.AddRange(Errores.Select(e => "• " + e));
            }
            if (Advertencias.Count > 0)
            {
                if (lineas.Count > 0) lineas.Add(string.Empty);
                lineas.Add("Recomendaciones:");
                lineas.AddRange(Advertencias.Select(a => "• " + a));
            }
            return string.Join(Environment.NewLine, lineas);
        }
    }

    public static class ValidacionEnsambleService
    {
        public static ResultadoValidacionEnsamble Validar(
            Ensamble ensamble,
            IReadOnlyCollection<SecuenciaEnsamble> secuencia,
            IReadOnlyCollection<Componente> componentes)
        {
            var resultado = new ResultadoValidacionEnsamble();
            if (string.IsNullOrWhiteSpace(ensamble.Nombre))
                resultado.Errores.Add("El ensamble no tiene nombre.");
            if (string.IsNullOrWhiteSpace(ensamble.UnidadTrabajo))
                resultado.Errores.Add("Falta la unidad de trabajo.");
            if (secuencia.Count == 0)
            {
                resultado.Errores.Add("La secuencia no contiene operaciones.");
                return resultado;
            }

            var componentesPorId = componentes.ToDictionary(c => c.ComponenteID);
            foreach (SecuenciaEnsamble paso in secuencia.OrderBy(s => s.OrdenSecuencia))
            {
                string etiqueta = $"Paso {paso.OrdenSecuencia}";
                if (paso.ComponenteID <= 0 || !componentesPorId.TryGetValue(paso.ComponenteID, out Componente? componente))
                {
                    resultado.Errores.Add($"{etiqueta}: componente no encontrado en inventario.");
                    continue;
                }
                if (componente.TiempoCiclo <= 0 && paso.TiempoEstimado <= 0)
                    resultado.Errores.Add($"{etiqueta}: falta el tiempo de ciclo.");
                if (string.IsNullOrWhiteSpace(componente.FotoRuta) || !File.Exists(componente.FotoRuta))
                    resultado.Errores.Add($"{etiqueta}: {componente.NumeroParte} no tiene imagen de componente.");
                if (string.IsNullOrWhiteSpace(paso.ImagenRuta) ||
                    !paso.ImagenRuta.Split(';', StringSplitOptions.RemoveEmptyEntries).Any(File.Exists))
                    resultado.Errores.Add($"{etiqueta}: falta la imagen del procedimiento.");
                if (string.IsNullOrWhiteSpace(paso.Referencia))
                    resultado.Advertencias.Add($"{etiqueta}: no tiene referencia.");
                if (paso.EstacionAsignada <= 0)
                    resultado.Advertencias.Add($"{etiqueta}: no tiene estación asignada.");
            }
            return resultado;
        }
    }
}

using System.Text;
using SmartBins.Modelos;
using SmartBins.Modelos.Produccion;

namespace SmartBins.Servicios.Balanceo
{
    public class LineBalancingService : ILineBalancingService
    {
        public PlanBalanceo Calcular(string ensambleNombre,
            IEnumerable<SecuenciaEnsamble> secuencia,
            IEnumerable<Componente> componentes,
            int numeroOperadores,
            ConfiguracionBalanceo? configuracion = null)
        {
            if (numeroOperadores <= 0)
                throw new ArgumentOutOfRangeException(nameof(numeroOperadores),
                    "La cantidad de operadores debe ser mayor que cero.");

            configuracion ??= new ConfiguracionBalanceo();
            ValidarConfiguracion(configuracion);
            var componentesPorId = componentes.GroupBy(c => c.ComponenteID)
                .ToDictionary(g => g.Key, g => g.First());
            var operaciones = secuencia.OrderBy(p => p.OrdenSecuencia)
                .Select(p => CrearOperacion(p, componentesPorId)).ToList();

            if (operaciones.Count == 0)
                throw new InvalidOperationException("El ensamble no contiene operaciones.");
            if (numeroOperadores > operaciones.Count)
                throw new InvalidOperationException(
                    $"No se pueden crear {numeroOperadores} estaciones con {operaciones.Count} operaciones. " +
                    "Cada estación requiere al menos una.");

            var recomendaciones = new List<string>();
            var claves = operaciones.Select(o => Normalizar(o.NumeroParte)).ToList();
            int distintas = claves.Where(c => c.Length > 0).Distinct().Count();
            decimal proporcion = (decimal)distintas / operaciones.Count;
            bool mezclaAlta = distintas >= configuracion.MinimoPartesDistintasParaMezclaAlta &&
                               proporcion >= configuracion.ProporcionMezclaAlta;
            var conteos = operaciones.GroupBy(o => Normalizar(o.NumeroParte))
                .Where(g => g.Key.Length > 0).ToDictionary(g => g.Key, g => g.Count());
            var distribuibles = conteos
                .Where(x => x.Value >= configuracion.UmbralRepeticionesParaDistribuir && !mezclaAlta)
                .Select(x => x.Key).ToHashSet();

            foreach (var grupo in conteos.Where(x => x.Value > 1))
            {
                if (distribuibles.Contains(grupo.Key))
                    recomendaciones.Add($"La parte {MostrarParte(operaciones, grupo.Key)} aparece {grupo.Value} veces; se distribuyó entre estaciones para balancear la carga.");
                else if (mezclaAlta && grupo.Value >= configuracion.UmbralRepeticionesParaDistribuir)
                    recomendaciones.Add($"La parte {MostrarParte(operaciones, grupo.Key)} aparece {grupo.Value} veces, pero se mantuvo en una estación porque el ensamble tiene mezcla alta.");
            }

            var similares = DetectarSimilares(operaciones, configuracion);
            foreach (var par in similares)
                recomendaciones.Add($"Partes parecidas detectadas: {par.ParteA} y {par.ParteB} ({par.Motivo}). El balance de tiempo tuvo prioridad y, cuando coincidieron en una estación, se procuró dejar dos operaciones entre ellas.");

            var estaciones = Distribuir(operaciones, numeroOperadores, distribuibles, similares);
            decimal tiempoTotal = operaciones.Sum(o => o.TiempoObjetivoSegundos);
            return new PlanBalanceo
            {
                EnsambleNombre = ensambleNombre, NumeroOperadores = numeroOperadores,
                TiempoTotalSegundos = tiempoTotal,
                TiempoObjetivoPorEstacion = tiempoTotal / numeroOperadores,
                TiempoCicloLinea = estaciones.Max(e => e.CargaTotalSegundos),
                Estaciones = estaciones, EsMezclaAlta = mezclaAlta,
                Recomendaciones = recomendaciones
            };
        }

        private static List<EstacionBalanceada> Distribuir(
            IReadOnlyList<OperacionPlanificada> operaciones, int cantidadEstaciones,
            HashSet<string> distribuibles, IReadOnlyList<PartesSimilares> similares)
        {
            var listas = Enumerable.Range(0, cantidadEstaciones)
                .Select(_ => new List<OperacionPlanificada>()).ToList();
            var cargas = new decimal[cantidadEstaciones];
            var estacionBloqueada = new Dictionary<string, int>();
            var clavesSimilares = similares
                .SelectMany(p => new[]
                {
                    (Normalizar(p.ParteA), Normalizar(p.ParteB)),
                    (Normalizar(p.ParteB), Normalizar(p.ParteA))
                }).GroupBy(x => x.Item1)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToHashSet());

            foreach (OperacionPlanificada operacion in operaciones)
            {
                string clave = Normalizar(operacion.NumeroParte);
                int estacion;
                if (clave.Length > 0 && !distribuibles.Contains(clave) &&
                    estacionBloqueada.TryGetValue(clave, out int fija))
                    estacion = fija;
                else
                {
                    estacion = Enumerable.Range(0, cantidadEstaciones)
                        // La similitud no debe provocar estaciones desbalanceadas. Primero se
                        // elige la menor carga y solo se usa la separación visual como desempate.
                        .OrderBy(i => cargas[i])
                        .ThenBy(i => ConflictosSimilaresRecientes(listas[i], clave, clavesSimilares))
                        .ThenBy(i => listas[i].Count > 0 && Normalizar(listas[i][^1].NumeroParte) == clave ? 1 : 0)
                        .ThenBy(i => i).First();
                    if (clave.Length > 0 && !distribuibles.Contains(clave))
                        estacionBloqueada[clave] = estacion;
                }
                var copia = CopiarEnEstacion(operacion, estacion + 1);
                listas[estacion].Add(copia);
                cargas[estacion] += copia.TiempoObjetivoSegundos;
            }

            for (int vacia = 0; vacia < cantidadEstaciones; vacia++)
            {
                if (listas[vacia].Count > 0) continue;
                var candidato = Enumerable.Range(0, cantidadEstaciones)
                    .SelectMany(i => listas[i].Select((o, indice) => new { Estacion = i, Indice = indice, Operacion = o }))
                    .Where(x => listas[x.Estacion].Count > 1)
                    .Where(x =>
                    {
                        string clave = Normalizar(x.Operacion.NumeroParte);
                        return clave.Length == 0 || distribuibles.Contains(clave) ||
                               operaciones.Count(o => Normalizar(o.NumeroParte) == clave) == 1;
                    })
                    .OrderByDescending(x => listas[x.Estacion].Count).FirstOrDefault();
                if (candidato == null)
                    throw new InvalidOperationException(
                        "No es posible ocupar todas las estaciones sin dividir un número de parte que no alcanza el umbral configurado. " +
                        "Reduce el número de estaciones o ajusta el umbral de repetición.");
                int origen = candidato.Estacion;
                var movida = candidato.Operacion;
                listas[origen].RemoveAt(candidato.Indice);
                cargas[origen] -= movida.TiempoObjetivoSegundos;
                var copia = CopiarEnEstacion(movida, vacia + 1);
                listas[vacia].Add(copia);
                cargas[vacia] += copia.TiempoObjetivoSegundos;
            }

            return listas.Select((lista, i) => new EstacionBalanceada
            {
                Numero = i + 1, Operaciones = lista.OrderBy(o => o.Orden).ToList(),
                CargaTotalSegundos = cargas[i]
            }).ToList();
        }

        private static int ConflictosSimilaresRecientes(List<OperacionPlanificada> estacion, string clave,
            Dictionary<string, HashSet<string>> similares)
        {
            if (!similares.TryGetValue(clave, out var relacionadas)) return 0;

            // Revisar las dos operaciones anteriores permite conservar las partes en la
            // estación que corresponda por carga sin colocarlas visualmente una junto a otra.
            return estacion.TakeLast(2)
                .Count(o => relacionadas.Contains(Normalizar(o.NumeroParte)));
        }

        private static List<PartesSimilares> DetectarSimilares(
            IReadOnlyList<OperacionPlanificada> operaciones, ConfiguracionBalanceo cfg)
        {
            var partes = operaciones.Select(o => o.NumeroParte)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var resultado = new List<PartesSimilares>();
            for (int i = 0; i < partes.Count; i++)
            for (int j = i + 1; j < partes.Count; j++)
            {
                string a = Normalizar(partes[i]);
                string b = Normalizar(partes[j]);
                string? motivo = null;
                if (a.Length == b.Length && a.Length >= 4 && Ordenar(a) == Ordenar(b))
                    motivo = "mismos caracteres en distinto orden";
                else if (SufijoIgual(a, b, cfg.LongitudSufijoCorto))
                    motivo = $"últimos {cfg.LongitudSufijoCorto} caracteres iguales";
                else if (a.Length >= cfg.LongitudSufijoLargo && b.Length >= cfg.LongitudSufijoLargo &&
                         Distancia(a[^cfg.LongitudSufijoLargo..], b[^cfg.LongitudSufijoLargo..]) <= cfg.DiferenciasPermitidasSufijoLargo)
                    motivo = $"últimos {cfg.LongitudSufijoLargo} caracteres parecidos";
                if (motivo != null) resultado.Add(new(partes[i], partes[j], motivo));
            }
            return resultado;
        }

        private static bool SufijoIgual(string a, string b, int longitud) =>
            longitud > 0 && a.Length >= longitud && b.Length >= longitud && a[^longitud..] == b[^longitud..];
        private static string Ordenar(string valor) => new(valor.OrderBy(c => c).ToArray());
        private static int Distancia(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            return d[a.Length, b.Length];
        }

        private static string Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in valor.ToUpperInvariant())
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            return sb.ToString();
        }
        private static string MostrarParte(IEnumerable<OperacionPlanificada> ops, string clave) =>
            ops.First(o => Normalizar(o.NumeroParte) == clave).NumeroParte;

        private static OperacionPlanificada CrearOperacion(SecuenciaEnsamble p,
            IReadOnlyDictionary<int, Componente> componentes)
        {
            componentes.TryGetValue(p.ComponenteID, out Componente? componente);
            string imagen = (p.ImagenRuta ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            return new OperacionPlanificada
            {
                OperacionId = p.SecuenciaID > 0 ? p.SecuenciaID : p.OrdenSecuencia,
                Orden = p.OrdenSecuencia, ComponenteID = p.ComponenteID,
                NombreComponente = p.NombreComponente,
                NumeroParte = componente?.NumeroParte ?? p.NumeroParte ?? string.Empty,
                Instruccion = p.EspecificacionComponente, Referencia = p.Referencia ?? string.Empty,
                Cantidad = 1, TiempoObjetivoSegundos = p.TiempoEstimado,
                FotoComponenteRuta = componente?.FotoRuta ?? string.Empty,
                ImagenProcedimientoRuta = imagen
            };
        }

        private static OperacionPlanificada CopiarEnEstacion(OperacionPlanificada o, int estacion) => new()
        {
            OperacionId = o.OperacionId, Orden = o.Orden, Estacion = estacion,
            ComponenteID = o.ComponenteID, NombreComponente = o.NombreComponente,
            NumeroParte = o.NumeroParte, Instruccion = o.Instruccion, Referencia = o.Referencia,
            Cantidad = o.Cantidad, TiempoObjetivoSegundos = o.TiempoObjetivoSegundos,
            FotoComponenteRuta = o.FotoComponenteRuta, ImagenProcedimientoRuta = o.ImagenProcedimientoRuta,
            Bin = o.Bin, Polaridad = o.Polaridad
        };

        private static void ValidarConfiguracion(ConfiguracionBalanceo c)
        {
            if (c.UmbralRepeticionesParaDistribuir < 2)
                throw new ArgumentOutOfRangeException(nameof(c.UmbralRepeticionesParaDistribuir));
            if (c.ProporcionMezclaAlta is <= 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(c.ProporcionMezclaAlta));
        }

        private sealed record PartesSimilares(string ParteA, string ParteB, string Motivo);
    }
}

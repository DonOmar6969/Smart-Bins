using ClosedXML.Excel;
using SmartBins.Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartBins.Datos
{
    // ==========================================
    // MODELOS DE RESULTADO
    // ==========================================
    public class FilaImportada
    {
        public int FilaOriginal { get; set; }
        public string NumeroParte { get; set; }
        public string Nombre { get; set; }   // Inferido de la descripción
        public string Descripcion { get; set; }
        public string Referencia { get; set; }   // Una sola referencia (R1, C2, etc.)
        public bool Seleccionada { get; set; } = true;
        public int EstacionAsignada { get; set; } = 0;
        public string Advertencia { get; set; } = string.Empty;
    }

    public class ResultadoImportacion
    {
        public List<FilaImportada> Filas { get; set; } = new();
        public string HojaUsada { get; set; }
        public List<string> Advertencias { get; set; } = new();
        public int FilasDescartadas { get; set; }
    }

    // ==========================================
    // SERVICIO PRINCIPAL
    // ==========================================
    public class ExcelImportadorService
    {
        // Columnas que buscamos — comparación fuzzy contra los headers reales
        private static readonly Dictionary<string, List<string>> _candidatos = new()
        {
            ["NivelExplosion"] = new() { "explosion level", "explosion", "level", "nivel", "bom level" },
            ["NumeroParte"] = new() { "component", "componente", "part number", "numero parte", "num parte" },
            ["Descripcion"] = new() { "object description", "description", "descripcion", "object desc", "desc" },
            ["Cantidad"] = new() { "comp. qty", "qty", "cantidad", "quantity", "comp qty" },
            ["Referencia"] = new() { "item text line 1", "item text", "reference", "referencia", "designator", "ref des" },
            ["Phantom"] = new() { "phantom item", "phantom", "fantasma" },
        };

        // Reglas de inferencia de nombre — ORDEN IMPORTA (más específico primero)
        private static readonly List<(string[] Palabras, string Nombre)> _reglasNombre = new()
        {
            (new[]{"xstr","2n3","2sd","2sc","2sa","bjt","mosfet","npn","pnp","trans gp"},       "Transistor"),
            (new[]{"di,ze","di,hv","diode schottky","diode switching","diodo"},                  "Diodo"),
            (new[]{"led ","led,"},                                                               "Diodo Led"),
            (new[]{"res,","res ","resistor","carbon film","ohm","potentiometer"},                "Resistencia"),
            (new[]{"cap aluminum","cap ceramic","cap film","cap,","capacitor","uf ","nf ","pf "}, "Capacitor"),
            (new[]{"xfmr","transformer","transformador"},                                        "Transformador"),
            (new[]{"inductor","ind,","choke"},                                                   "Inductor"),
            (new[]{"ic ","ic,","opto","optoisolator","volt reg","shunt reg","lm3","lm7"},        "Circuito Integrado"),
            (new[]{"wire","buss wire"},                                                          "Cable"),
            (new[]{"pcb,","pcb "},                                                              "PCB"),
            (new[]{"switch","sw,"},                                                              "Switch"),
            (new[]{"relay"},                                                                     "Relay"),
            (new[]{"terminal","faston"},                                                         "Terminal"),
            (new[]{"standoff","eyelet","clips","bobbin","heatsink","herraje"},                   "Herraje"),
            (new[]{"assy,","assy ","subensamble"},                                               "Subensamble"),
        };

        // Valores en columna Referencia que indican fila a descartar
        private static readonly string[] _valoresDescartar = { "phantom", "pcb smt", "see bom" };

        // ==========================================
        // PUNTO DE ENTRADA
        // ==========================================
        public ResultadoImportacion ImportarDesdeExcel(string rutaArchivo)
        {
            var resultado = new ResultadoImportacion();

            using var wb = new XLWorkbook(rutaArchivo);
            var hoja = wb.Worksheets.OrderByDescending(h => h.LastRowUsed()?.RowNumber() ?? 0).First();
            resultado.HojaUsada = hoja.Name;

            // Detectar fila de encabezados (busca en las primeras 10 filas)
            int filaHeader = DetectarFilaHeaders(hoja);
            if (filaHeader < 0)
            {
                resultado.Advertencias.Add("No se encontraron encabezados reconocibles en el archivo.");
                return resultado;
            }

            // Mapear columnas
            var mapeo = MapearColumnas(hoja, filaHeader);

            // Verificar columnas mínimas
            if (!mapeo.ContainsKey("NumeroParte") || !mapeo.ContainsKey("Descripcion"))
            {
                resultado.Advertencias.Add("No se pudieron identificar las columnas de Número de Parte o Descripción.");
                return resultado;
            }

            bool tieneNivel = mapeo.ContainsKey("NivelExplosion");
            bool tieneRef = mapeo.ContainsKey("Referencia");
            bool tienePhantom = mapeo.ContainsKey("Phantom");
            bool tieneCantidad = mapeo.ContainsKey("Cantidad");

            int totalFilas = hoja.LastRowUsed()?.RowNumber() ?? filaHeader;

            for (int r = filaHeader + 1; r <= totalFilas; r++)
            {
                string ObtenerCelda(string campo)
                {
                    if (!mapeo.ContainsKey(campo)) return string.Empty;
                    return hoja.Cell(r, mapeo[campo]).GetString().Trim();
                }

                // Filtrar por nivel de explosión — solo L.1
                if (tieneNivel)
                {
                    string nivel = ObtenerCelda("NivelExplosion");
                    if (string.IsNullOrWhiteSpace(nivel)) { resultado.FilasDescartadas++; continue; }
                    if (nivel != "L.1") { resultado.FilasDescartadas++; continue; }
                }

                string numeroParte = ObtenerCelda("NumeroParte");
                string descripcion = ObtenerCelda("Descripcion");

                // Fila vacía
                if (string.IsNullOrWhiteSpace(numeroParte) && string.IsNullOrWhiteSpace(descripcion))
                {
                    resultado.FilasDescartadas++;
                    continue;
                }

                // Filtrar phantoms
                string refTexto = tieneRef ? ObtenerCelda("Referencia") : string.Empty;
                if (tienePhantom && ObtenerCelda("Phantom").Equals("X", StringComparison.OrdinalIgnoreCase))
                {
                    resultado.FilasDescartadas++;
                    continue;
                }
                if (_valoresDescartar.Any(v => refTexto.ToLowerInvariant().Contains(v)))
                {
                    resultado.FilasDescartadas++;
                    continue;
                }

                // Inferir nombre desde descripción
                string nombre = InferirNombre(descripcion);

                // Separar referencias (puede haber varias: "R4, R9" → dos filas)
                var referencias = SepararReferencias(refTexto);

                // Si no hay referencias, crear una sola fila sin referencia
                if (!referencias.Any())
                    referencias = new List<string> { string.Empty };

                foreach (var referencia in referencias)
                {
                    string advertencia = string.Empty;
                    if (string.IsNullOrWhiteSpace(numeroParte)) advertencia = "Sin número de parte";
                    else if (nombre == "Componente") advertencia = "Tipo no reconocido";

                    resultado.Filas.Add(new FilaImportada
                    {
                        FilaOriginal = r,
                        NumeroParte = numeroParte,
                        Nombre = nombre,
                        Descripcion = descripcion,
                        Referencia = referencia,
                        Seleccionada = true,
                        Advertencia = advertencia
                    });
                }
            }

            if (!resultado.Filas.Any())
                resultado.Advertencias.Add($"No se encontraron filas de primer ensamble (L.1). Se descartaron {resultado.FilasDescartadas} filas de subensambles o phantoms.");

            return resultado;
        }

        // ==========================================
        // DETECTAR FILA DE ENCABEZADOS
        // ==========================================
        private int DetectarFilaHeaders(IXLWorksheet hoja)
        {
            int maxBusqueda = Math.Min(15, hoja.LastRowUsed()?.RowNumber() ?? 1);
            for (int r = 1; r <= maxBusqueda; r++)
            {
                var celdas = hoja.Row(r).CellsUsed()
                                 .Select(c => c.GetString().Trim().ToLowerInvariant())
                                 .ToList();
                int coincidencias = 0;
                foreach (var celda in celdas)
                    foreach (var lista in _candidatos.Values)
                        if (lista.Any(c => SimilitudDice(celda, c) > 0.55)) { coincidencias++; break; }

                if (coincidencias >= 3) return r;
            }
            return -1;
        }

        // ==========================================
        // MAPEAR COLUMNAS (fuzzy)
        // ==========================================
        private Dictionary<string, int> MapearColumnas(IXLWorksheet hoja, int filaHeader)
        {
            var resultado = new Dictionary<string, int>();
            var celdas = hoja.Row(filaHeader).CellsUsed().ToList();

            foreach (var campo in _candidatos.Keys)
            {
                double mejorScore = 0.45;
                int mejorCol = -1;

                foreach (var celda in celdas)
                {
                    string texto = celda.GetString().Trim().ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(texto)) continue;
                    foreach (var candidato in _candidatos[campo])
                    {
                        double score = SimilitudDice(texto, candidato);
                        if (score > mejorScore) { mejorScore = score; mejorCol = celda.Address.ColumnNumber; }
                    }
                }
                if (mejorCol > 0) resultado[campo] = mejorCol;
            }
            return resultado;
        }

        // ==========================================
        // INFERIR NOMBRE DESDE DESCRIPCIÓN
        // ==========================================
        private string InferirNombre(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion)) return "Componente";
            string d = descripcion.ToLowerInvariant();

            foreach (var (palabras, nombre) in _reglasNombre)
                if (palabras.Any(p => d.Contains(p)))
                    return nombre;

            return "Componente";
        }

        // ==========================================
        // SEPARAR REFERENCIAS MÚLTIPLES
        // "R4, R9" → ["R4", "R9"]
        // "D1, D21,D4" → ["D1", "D21", "D4"]
        // ==========================================
        private List<string> SepararReferencias(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return new List<string>();

            return texto
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .SelectMany(ExpandirRangoReferencia)
                .ToList();
        }

        private static IEnumerable<string> ExpandirRangoReferencia(string referencia)
        {
            // Se exige el prefijo en ambos extremos para no confundir con un rango
            // las referencias que legítimamente contienen un guion.
            Match rango = Regex.Match(
                referencia,
                @"^(?<prefijo>[A-Za-z]+)\s*(?<inicio>\d+)\s*[-–—]\s*" +
                @"(?<prefijoFin>[A-Za-z]+)\s*(?<fin>\d+)$",
                RegexOptions.CultureInvariant);

            if (!rango.Success ||
                !rango.Groups["prefijo"].Value.Equals(
                    rango.Groups["prefijoFin"].Value,
                    StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(rango.Groups["inicio"].Value, out int inicio) ||
                !int.TryParse(rango.Groups["fin"].Value, out int fin))
            {
                return new[] { referencia };
            }

            // Protege la secuencia ante rangos excesivos causados por datos erróneos.
            if (Math.Abs((long)fin - inicio) > 1000)
                return new[] { referencia };

            string prefijo = rango.Groups["prefijo"].Value;
            int paso = fin >= inicio ? 1 : -1;
            int cantidad = Math.Abs(fin - inicio) + 1;

            return Enumerable.Range(0, cantidad)
                .Select(indice => $"{prefijo}{inicio + (indice * paso)}");
        }

        // ==========================================
        // COEFICIENTE DE DICE (similitud de bigramas)
        // ==========================================
        public static double SimilitudDice(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
            if (a == b) return 1.0;

            var biA = Bigramas(a);
            var biB = Bigramas(b);
            if (biA.Count == 0 || biB.Count == 0) return 0;

            int interseccion = biA.Intersect(biB).Count();
            return (2.0 * interseccion) / (biA.Count + biB.Count);
        }

        private static List<string> Bigramas(string s)
        {
            var lista = new List<string>();
            for (int i = 0; i < s.Length - 1; i++)
                lista.Add(s.Substring(i, 2));
            return lista;
        }
    }
}

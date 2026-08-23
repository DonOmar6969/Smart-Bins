using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using SmartBins.Modelos.Produccion;
using System.IO;

namespace SmartBins.Servicios.Reportes
{
    public class ReporteLiberacionPdfService : IReporteLiberacionPdfService
    {
        private static readonly Color Azul = Color.Parse("#0066CC");
        private static readonly Color AzulClaro = Color.Parse("#EAF4FF");
        private static readonly Color GrisClaro = Color.Parse("#F1F3F5");
        private static readonly Color GrisTexto = Color.Parse("#465563");

        public void Generar(string rutaArchivo, ReporteLiberacionDatos datos)
        {
            if (datos.Plan.Estaciones.Count == 0)
                throw new InvalidOperationException("El plan no contiene estaciones.");

            string? carpeta = Path.GetDirectoryName(rutaArchivo);
            if (!string.IsNullOrWhiteSpace(carpeta))
                Directory.CreateDirectory(carpeta);

            Document documento = CrearDocumento(datos);
            var renderer = new PdfDocumentRenderer
            {
                Document = documento
            };
            renderer.RenderDocument();
            renderer.PdfDocument.Info.Title = $"Reporte de liberación - {datos.Modelo}";
            renderer.PdfDocument.Info.Subject = "Plan de balanceo y secuencia de producción";
            renderer.PdfDocument.Info.Author = "SMART BINS";
            renderer.PdfDocument.Save(rutaArchivo);
        }

        private static Document CrearDocumento(ReporteLiberacionDatos datos)
        {
            var documento = new Document();
            documento.Info.Title = $"Reporte de liberación - {datos.Modelo}";
            documento.Info.Author = "SMART BINS";

            Style normal = documento.Styles[StyleNames.Normal]!;
            normal.Font.Name = "Arial";
            normal.Font.Size = 9;
            normal.Font.Color = GrisTexto;
            normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);

            Style titulo1 = documento.Styles[StyleNames.Heading1]!;
            titulo1.Font.Name = "Arial";
            titulo1.Font.Size = 15;
            titulo1.Font.Bold = true;
            titulo1.Font.Color = Azul;
            titulo1.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
            titulo1.ParagraphFormat.SpaceAfter = Unit.FromPoint(7);
            titulo1.ParagraphFormat.KeepWithNext = true;

            Section seccion = documento.AddSection();
            seccion.PageSetup.PageFormat = PageFormat.A4;
            seccion.PageSetup.Orientation = Orientation.Landscape;
            seccion.PageSetup.TopMargin = Unit.FromCentimeter(2.4);
            seccion.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);
            seccion.PageSetup.LeftMargin = Unit.FromCentimeter(1.4);
            seccion.PageSetup.RightMargin = Unit.FromCentimeter(1.4);
            seccion.PageSetup.HeaderDistance = Unit.FromCentimeter(0.7);

            CrearEncabezado(seccion);
            CrearPiePagina(seccion);
            CrearPortadaResumen(seccion, datos);
            CrearInventarioComponentes(seccion, datos.Plan);
            CrearSecuenciaPorEstacion(seccion, datos.Plan);
            return documento;
        }

        private static void CrearEncabezado(Section seccion)
        {
            HeaderFooter encabezado = seccion.Headers.Primary;
            var tabla = encabezado.AddTable();
            tabla.Borders.Bottom.Width = Unit.FromPoint(1);
            tabla.Borders.Bottom.Color = Azul;
            tabla.AddColumn(Unit.FromCentimeter(12.5));
            tabla.AddColumn(Unit.FromCentimeter(14));
            Row fila = tabla.AddRow();

            Paragraph marca = fila.Cells[0].AddParagraph();
            FormattedText negrita = marca.AddFormattedText("SMART BINS", TextFormat.Bold);
            negrita.Font.Size = 13;
            negrita.Font.Color = Azul;

            Paragraph tipo = fila.Cells[1].AddParagraph("REPORTE DE LIBERACIÓN DE PRODUCCIÓN");
            tipo.Format.Alignment = ParagraphAlignment.Right;
            tipo.Format.Font.Bold = true;
            tipo.Format.Font.Color = GrisTexto;
        }

        private static void CrearPiePagina(Section seccion)
        {
            HeaderFooter pie = seccion.Footers.Primary;
            Paragraph parrafo = pie.AddParagraph();
            parrafo.Format.Alignment = ParagraphAlignment.Center;
            parrafo.Format.Font.Size = 8;
            parrafo.Format.Font.Color = Colors.Gray;
            parrafo.AddText("Documento generado por SMART BINS  |  Página ");
            parrafo.AddPageField();
            parrafo.AddText(" de ");
            parrafo.AddNumPagesField();
        }

        private static void CrearPortadaResumen(Section seccion, ReporteLiberacionDatos datos)
        {
            Paragraph titulo = seccion.AddParagraph();
            titulo.Format.SpaceBefore = Unit.FromPoint(10);
            titulo.Format.SpaceAfter = Unit.FromPoint(3);
            FormattedText textoTitulo = titulo.AddFormattedText("Reporte de liberación", TextFormat.Bold);
            textoTitulo.Font.Size = 24;
            textoTitulo.Font.Color = Azul;

            Paragraph subtitulo = seccion.AddParagraph(
                "Configuración autorizada para la corrida simulada de producción.");
            subtitulo.Format.Font.Size = 11;
            subtitulo.Format.SpaceAfter = Unit.FromPoint(14);

            var datosGenerales = seccion.AddTable();
            datosGenerales.Borders.Width = Unit.FromPoint(0.5);
            datosGenerales.Borders.Color = Color.Parse("#C8D0D8");
            for (int i = 0; i < 4; i++)
                datosGenerales.AddColumn(Unit.FromCentimeter(6.6));

            AgregarParDatos(datosGenerales.AddRow(),
                "Modelo / ensamble", datos.Modelo,
                "Línea / unidad de trabajo", Valor(datos.Linea));
            AgregarParDatos(datosGenerales.AddRow(),
                "Fecha y hora", datos.FechaGeneracion.ToString("dd/MM/yyyy HH:mm:ss"),
                "Unidades planeadas", datos.CantidadUnidades.ToString());
            AgregarParDatos(datosGenerales.AddRow(),
                "Operadores / estaciones", datos.Plan.NumeroOperadores.ToString(),
                "Total de operaciones",
                datos.Plan.Estaciones.Sum(e => e.Operaciones.Count).ToString());

            seccion.AddParagraph("Resumen de balance", StyleNames.Heading1);
            var balance = seccion.AddTable();
            balance.Borders.Width = Unit.FromPoint(0.5);
            balance.Borders.Color = Color.Parse("#C8D0D8");
            balance.AddColumn(Unit.FromCentimeter(6.6));
            balance.AddColumn(Unit.FromCentimeter(6.6));
            balance.AddColumn(Unit.FromCentimeter(6.6));
            balance.AddColumn(Unit.FromCentimeter(6.6));

            Row encabezado = balance.AddRow();
            encabezado.Shading.Color = Azul;
            AgregarCeldaEncabezado(encabezado.Cells[0], "Tiempo total");
            AgregarCeldaEncabezado(encabezado.Cells[1], "Objetivo por estación");
            AgregarCeldaEncabezado(encabezado.Cells[2], "Ciclo de línea");
            AgregarCeldaEncabezado(encabezado.Cells[3], "Cuello de botella");

            Row valores = balance.AddRow();
            valores.Cells[0].AddParagraph($"{datos.Plan.TiempoTotalSegundos:0.##} s");
            valores.Cells[1].AddParagraph($"{datos.Plan.TiempoObjetivoPorEstacion:0.##} s");
            valores.Cells[2].AddParagraph($"{datos.Plan.TiempoCicloLinea:0.##} s");
            EstacionBalanceada cuello = datos.Plan.Estaciones
                .OrderByDescending(e => e.CargaTotalSegundos).First();
            valores.Cells[3].AddParagraph($"Estación {cuello.Numero} ({cuello.CargaTotalSegundos:0.##} s)");
            FormatearFilaValores(valores);

            seccion.AddParagraph("Distribución por estación", StyleNames.Heading1);
            var distribucion = CrearTablaBase(seccion,
                ("Estación", 3.0),
                ("Operaciones", 5.0),
                ("Carga", 5.0),
                ("Desviación contra objetivo", 7.0),
                ("Utilización", 6.4));

            foreach (EstacionBalanceada estacion in datos.Plan.Estaciones)
            {
                Row fila = distribucion.AddRow();
                decimal desviacion = estacion.CargaTotalSegundos -
                                     datos.Plan.TiempoObjetivoPorEstacion;
                decimal utilizacion = datos.Plan.TiempoCicloLinea <= 0
                    ? 0
                    : estacion.CargaTotalSegundos / datos.Plan.TiempoCicloLinea * 100;
                fila.Cells[0].AddParagraph(estacion.Numero.ToString());
                fila.Cells[1].AddParagraph(estacion.Operaciones.Count.ToString());
                fila.Cells[2].AddParagraph($"{estacion.CargaTotalSegundos:0.##} s");
                fila.Cells[3].AddParagraph($"{(desviacion >= 0 ? "+" : "")}{desviacion:0.##} s");
                fila.Cells[4].AddParagraph($"{utilizacion:0.#}%");
                FormatearFilaValores(fila);
            }
        }

        private static void CrearInventarioComponentes(Section seccion, PlanBalanceo plan)
        {
            seccion.AddParagraph("Componentes incluidos", StyleNames.Heading1);
            var componentes = plan.Estaciones
                .SelectMany(e => e.Operaciones)
                .GroupBy(o => new { o.ComponenteID, o.NumeroParte, o.NombreComponente })
                .Select(g => new
                {
                    g.Key.NumeroParte,
                    g.Key.NombreComponente,
                    CantidadOperaciones = g.Count(),
                    Referencias = string.Join(", ", g.Select(x => x.Referencia)
                        .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                })
                .OrderBy(c => c.NombreComponente)
                .ToList();

            var tabla = CrearTablaBase(seccion,
                ("No. de parte", 5.0),
                ("Componente", 8.5),
                ("Apariciones", 3.4),
                ("Referencias", 9.5));

            foreach (var componente in componentes)
            {
                Row fila = tabla.AddRow();
                fila.Cells[0].AddParagraph(Valor(componente.NumeroParte));
                fila.Cells[1].AddParagraph(Valor(componente.NombreComponente));
                fila.Cells[2].AddParagraph(componente.CantidadOperaciones.ToString());
                fila.Cells[3].AddParagraph(Valor(componente.Referencias));
                FormatearFilaValores(fila);
            }
        }

        private static void CrearSecuenciaPorEstacion(Section seccion, PlanBalanceo plan)
        {
            foreach (EstacionBalanceada estacion in plan.Estaciones)
            {
                Paragraph titulo = seccion.AddParagraph(
                    $"Secuencia - Estación {estacion.Numero}", StyleNames.Heading1);
                titulo.AddFormattedText(
                    $"   |   Carga: {estacion.CargaTotalSegundos:0.##} s",
                    TextFormat.NotBold);

                var tabla = CrearTablaBase(seccion,
                    ("Orden", 2.0),
                    ("Referencia", 2.8),
                    ("No. de parte", 4.0),
                    ("Componente", 5.0),
                    ("Cant.", 1.6),
                    ("Tiempo", 2.2),
                    ("Instrucción", 8.8));

                foreach (OperacionPlanificada operacion in estacion.Operaciones)
                {
                    Row fila = tabla.AddRow();
                    fila.TopPadding = Unit.FromPoint(3);
                    fila.BottomPadding = Unit.FromPoint(3);
                    fila.Cells[0].AddParagraph(operacion.Orden.ToString());
                    fila.Cells[1].AddParagraph(Valor(operacion.Referencia));
                    fila.Cells[2].AddParagraph(Valor(operacion.NumeroParte));
                    fila.Cells[3].AddParagraph(Valor(operacion.NombreComponente));
                    fila.Cells[4].AddParagraph(operacion.Cantidad.ToString());
                    fila.Cells[5].AddParagraph($"{operacion.TiempoObjetivoSegundos:0.##} s");
                    fila.Cells[6].AddParagraph(Valor(operacion.Instruccion));
                    FormatearFilaValores(fila);
                }
            }
        }

        private static void CrearAprobacion(Section seccion)
        {
            seccion.AddParagraph("Liberación", StyleNames.Heading1);
            Paragraph nota = seccion.AddParagraph(
                "Este documento representa la distribución calculada al momento de su generación. " +
                "Cualquier cambio posterior al ensamble, cantidad de operadores o secuencia requiere " +
                "generar un nuevo reporte de liberación.");
            nota.Format.SpaceAfter = Unit.FromPoint(18);

            var firmas = seccion.AddTable();
            firmas.AddColumn(Unit.FromCentimeter(8.2));
            firmas.AddColumn(Unit.FromCentimeter(1.0));
            firmas.AddColumn(Unit.FromCentimeter(8.2));
            firmas.AddColumn(Unit.FromCentimeter(1.0));
            firmas.AddColumn(Unit.FromCentimeter(8.2));
            Row linea = firmas.AddRow();
            linea.Cells[0].Borders.Bottom.Width = Unit.FromPoint(0.7);
            linea.Cells[2].Borders.Bottom.Width = Unit.FromPoint(0.7);
            linea.Cells[4].Borders.Bottom.Width = Unit.FromPoint(0.7);
            Row etiquetas = firmas.AddRow();
            etiquetas.Cells[0].AddParagraph("Ingeniería de procesos / Fecha");
            etiquetas.Cells[2].AddParagraph("Supervisor de producción / Fecha");
            etiquetas.Cells[4].AddParagraph("Calidad / Fecha");
            foreach (int indice in new[] { 0, 2, 4 })
            {
                etiquetas.Cells[indice].Format.Alignment = ParagraphAlignment.Center;
                etiquetas.Cells[indice].Format.Font.Size = 8;
                etiquetas.Cells[indice].Format.Font.Color = Colors.Gray;
            }
        }

        private static Table CrearTablaBase(
            Section seccion,
            params (string Titulo, double AnchoCm)[] columnas)
        {
            Table tabla = seccion.AddTable();
            tabla.Borders.Width = Unit.FromPoint(0.35);
            tabla.Borders.Color = Color.Parse("#C8D0D8");

            foreach ((string _, double ancho) in columnas)
                tabla.AddColumn(Unit.FromCentimeter(ancho));

            Row encabezado = tabla.AddRow();
            encabezado.HeadingFormat = true;
            encabezado.Shading.Color = Azul;
            for (int i = 0; i < columnas.Length; i++)
                AgregarCeldaEncabezado(encabezado.Cells[i], columnas[i].Titulo);
            return tabla;
        }

        private static void AgregarParDatos(
            Row fila,
            string etiqueta1,
            string valor1,
            string etiqueta2,
            string valor2)
        {
            fila.Cells[0].Shading.Color = GrisClaro;
            fila.Cells[2].Shading.Color = GrisClaro;
            fila.Cells[0].AddParagraph(etiqueta1).Format.Font.Bold = true;
            fila.Cells[1].AddParagraph(Valor(valor1));
            fila.Cells[2].AddParagraph(etiqueta2).Format.Font.Bold = true;
            fila.Cells[3].AddParagraph(Valor(valor2));
            FormatearFilaValores(fila);
        }

        private static void AgregarCeldaEncabezado(Cell celda, string texto)
        {
            Paragraph parrafo = celda.AddParagraph(texto);
            parrafo.Format.Font.Bold = true;
            parrafo.Format.Font.Color = Colors.White;
            parrafo.Format.Alignment = ParagraphAlignment.Center;
            celda.VerticalAlignment = VerticalAlignment.Center;
        }

        private static void FormatearFilaValores(Row fila)
        {
            fila.TopPadding = Unit.FromPoint(4);
            fila.BottomPadding = Unit.FromPoint(4);
            fila.VerticalAlignment = VerticalAlignment.Center;
        }

        private static string Valor(string? valor)
            => string.IsNullOrWhiteSpace(valor) ? "No especificado" : valor;
    }
}

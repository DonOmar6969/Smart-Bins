import fs from "node:fs/promises";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const OUT = "output/presentations";

async function writeBlob(path, blob) {
  await fs.writeFile(path, new Uint8Array(await blob.arrayBuffer()));
}

function addText(slide, name, text, position, style) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    name,
    position,
    fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
  });
  shape.text = text;
  shape.text.style = style;
  return shape;
}

async function main() {
  await fs.mkdir(OUT, { recursive: true });

  const deck = Presentation.create({ slideSize: { width: 1280, height: 720 } });
  const slide = deck.slides.add();
  slide.background.fill = "#F6F4EF";

  slide.shapes.add({
    geometry: "rect",
    name: "header-band",
    position: { left: 0, top: 0, width: 1280, height: 94 },
    fill: "#005B7F",
    line: { style: "solid", fill: "#005B7F", width: 0 },
  });
  slide.shapes.add({
    geometry: "rect",
    name: "header-accent",
    position: { left: 0, top: 94, width: 1280, height: 7 },
    fill: "#E8873A",
    line: { style: "solid", fill: "#E8873A", width: 0 },
  });

  addText(slide, "title", "Smart Bins convierte un modelo en producción guiada", { left: 58, top: 20, width: 1020, height: 52 }, { fontFamily: "Aptos Display", fontSize: 34, bold: true, color: "#FFFFFF" });
  addText(slide, "subtitle", "Un flujo digital, desde la preparación de ingeniería hasta la ejecución del operador", { left: 60, top: 120, width: 1080, height: 32 }, { fontFamily: "Aptos", fontSize: 19, color: "#35505C" });

  const steps = [
    { n: "01", title: "Preparar el modelo", body: "Componentes, bins, imágenes y tiempos de ciclo", color: "#D85A6A", tag: "INGENIERÍA" },
    { n: "02", title: "Definir el proceso", body: "Crear o importar la secuencia de ensamble", color: "#E8873A", tag: "INGENIERÍA" },
    { n: "03", title: "Balancear la línea", body: "Elegir operadores y distribuir la carga automáticamente", color: "#82A83C", tag: "PRODUCCIÓN" },
    { n: "04", title: "Guiar la ejecución", body: "Instrucciones visuales, avance y resultados de la corrida", color: "#008B89", tag: "PRODUCCIÓN" },
  ];

  const left = 58;
  const top = 196;
  const cardW = 260;
  const cardH = 294;
  const gap = 38;

  for (let i = 0; i < steps.length; i++) {
    const s = steps[i];
    const x = left + i * (cardW + gap);
    slide.shapes.add({
      geometry: "roundRect",
      name: `step-card-${i + 1}`,
      position: { left: x, top, width: cardW, height: cardH },
      fill: "#FFFFFF",
      line: { style: "solid", fill: "#D8E0E2", width: 1 },
      borderRadius: "rounded-xl",
      shadow: "shadow-sm",
    });
    slide.shapes.add({
      geometry: "roundRect",
      name: `step-number-${i + 1}`,
      position: { left: x + 22, top: top + 22, width: 60, height: 60 },
      fill: s.color,
      line: { style: "solid", fill: s.color, width: 0 },
      borderRadius: "rounded-xl",
    });
    addText(slide, `step-number-text-${i + 1}`, s.n, { left: x + 22, top: top + 34, width: 60, height: 30 }, { fontFamily: "Aptos Display", fontSize: 20, bold: true, color: "#FFFFFF", alignment: "center" });
    addText(slide, `step-tag-${i + 1}`, s.tag, { left: x + 98, top: top + 35, width: 134, height: 24 }, { fontFamily: "Aptos", fontSize: 11, bold: true, color: s.color, alignment: "right" });
    addText(slide, `step-title-${i + 1}`, s.title, { left: x + 22, top: top + 110, width: 216, height: 62 }, { fontFamily: "Aptos Display", fontSize: 24, bold: true, color: "#15333E" });
    addText(slide, `step-body-${i + 1}`, s.body, { left: x + 22, top: top + 188, width: 214, height: 76 }, { fontFamily: "Aptos", fontSize: 17, color: "#536A73" });

    if (i < steps.length - 1) {
      slide.shapes.add({
        geometry: "chevron",
        name: `flow-arrow-${i + 1}`,
        position: { left: x + cardW + 8, top: top + 126, width: 22, height: 42 },
        fill: "#AAB8BC",
        line: { style: "solid", fill: "#AAB8BC", width: 0 },
      });
    }
  }

  slide.shapes.add({
    geometry: "roundRect",
    name: "result-band",
    position: { left: 58, top: 532, width: 1154, height: 112 },
    fill: "#E4EEF0",
    line: { style: "solid", fill: "#BDD1D6", width: 1 },
    borderRadius: "rounded-xl",
  });
  addText(slide, "result-label", "RESULTADO", { left: 82, top: 554, width: 146, height: 26 }, { fontFamily: "Aptos", fontSize: 13, bold: true, color: "#005B7F" });
  addText(slide, "result-text", "Cambios de modelo más ágiles  •  trabajo estandarizado  •  base escalable para estaciones inteligentes", { left: 82, top: 582, width: 1065, height: 34 }, { fontFamily: "Aptos Display", fontSize: 21, bold: true, color: "#15333E" });
  addText(slide, "footer", "SMART BINS  |  Staff concept sample", { left: 970, top: 672, width: 242, height: 18 }, { fontFamily: "Aptos", fontSize: 10, color: "#75888F", alignment: "right" });

  const png = await deck.export({ slide, format: "png", scale: 2 });
  await writeBlob(`${OUT}/SmartBins_Staff_Slide_Muestra.png`, png);
  const layout = await slide.export({ format: "layout" });
  await fs.writeFile(`${OUT}/SmartBins_Staff_Slide_Muestra.layout.json`, await layout.text());
  const pptx = await PresentationFile.exportPptx(deck);
  await pptx.save(`${OUT}/SmartBins_Staff_Slide_Muestra.pptx`);

  const inspection = await deck.inspect({ kind: "slide,textbox,shape", maxChars: 12000 });
  await fs.writeFile(`${OUT}/SmartBins_Staff_Slide_Muestra.inspect.ndjson`, inspection.ndjson);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});

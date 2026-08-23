using SmartBins.Modelos;
using System.IO;
using System.Text.Json;

namespace SmartBins.Servicios
{
    public sealed class ControlEnsamble
    {
        public int Revision { get; set; } = 1;
        public string Estado { get; set; } = "Aprobado";
        public DateTime UltimaModificacion { get; set; } = DateTime.Now;
        public string Usuario { get; set; } = Environment.UserName;
    }

    public static class ControlEnsambleService
    {
        private static readonly string Carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartBins");
        private static readonly string RutaControl = Path.Combine(Carpeta, "control-ensambles.json");
        private static readonly string RutaAuditoria = Path.Combine(Carpeta, "auditoria.jsonl");

        public static void Aplicar(Ensamble ensamble)
        {
            Dictionary<string, ControlEnsamble> controles = Cargar();
            if (controles.TryGetValue(ensamble.Nombre, out ControlEnsamble? control))
            {
                ensamble.Revision = control.Revision;
                // Los registros históricos guardados como "Borrador" corresponden a
                // ensambles ya aprobados y se presentan con el estado correcto.
                ensamble.Estado = string.Equals(control.Estado, "Borrador",
                    StringComparison.OrdinalIgnoreCase) ? "Aprobado" : control.Estado;
                ensamble.UltimaModificacion = control.UltimaModificacion;
            }
        }

        public static void RegistrarGuardado(string nombre, bool esNuevo)
        {
            var controles = Cargar();
            if (!controles.TryGetValue(nombre, out ControlEnsamble? control))
                control = new ControlEnsamble();
            else if (!esNuevo)
                control.Revision++;
            control.Estado = "Aprobado";
            control.UltimaModificacion = DateTime.Now;
            control.Usuario = Environment.UserName;
            controles[nombre] = control;
            Guardar(controles);
            Auditar(nombre, esNuevo ? "Ensamble creado y aprobado" : "Nueva revisión aprobada", control);
        }

        public static void RegistrarLiberacion(string nombre)
        {
            var controles = Cargar();
            if (!controles.TryGetValue(nombre, out ControlEnsamble? control))
                control = new ControlEnsamble();
            control.Estado = "Liberado";
            control.UltimaModificacion = DateTime.Now;
            control.Usuario = Environment.UserName;
            controles[nombre] = control;
            Guardar(controles);
            Auditar(nombre, "Ensamble liberado", control);
        }

        private static Dictionary<string, ControlEnsamble> Cargar()
        {
            try
            {
                if (!File.Exists(RutaControl))
                    return new(StringComparer.OrdinalIgnoreCase);
                return JsonSerializer.Deserialize<Dictionary<string, ControlEnsamble>>(
                    File.ReadAllText(RutaControl)) ?? new(StringComparer.OrdinalIgnoreCase);
            }
            catch { return new(StringComparer.OrdinalIgnoreCase); }
        }

        private static void Guardar(Dictionary<string, ControlEnsamble> controles)
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(RutaControl, JsonSerializer.Serialize(controles,
                new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void Auditar(string ensamble, string accion, ControlEnsamble control)
        {
            Directory.CreateDirectory(Carpeta);
            string evento = JsonSerializer.Serialize(new
            {
                Fecha = DateTime.Now,
                Usuario = Environment.UserName,
                Rol = "Editor",
                Ensamble = ensamble,
                Accion = accion,
                Revision = control.Revision,
                Estado = control.Estado
            });
            File.AppendAllText(RutaAuditoria, evento + Environment.NewLine);
        }
    }
}

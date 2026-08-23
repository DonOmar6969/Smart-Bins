using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartBins.Servicios
{
    public static class EntradaNumericaService
    {
        public static void RestringirAEnteros(params TextBox[] campos)
        {
            foreach (TextBox campo in campos)
            {
                campo.PreviewTextInput += (_, e) =>
                    e.Handled = !EsEntero(ProyectarTexto(campo, e.Text));
                DataObject.AddPastingHandler(campo, (s, e) => ValidarPegado(s, e, EsEntero));
            }
        }

        public static void RestringirADecimal(params TextBox[] campos)
        {
            foreach (TextBox campo in campos)
            {
                campo.PreviewTextInput += (_, e) =>
                    e.Handled = !EsDecimal(ProyectarTexto(campo, e.Text));
                DataObject.AddPastingHandler(campo, (s, e) => ValidarPegado(s, e, EsDecimal));
            }
        }

        private static string ProyectarTexto(TextBox campo, string entrada) =>
            campo.Text.Remove(campo.SelectionStart, campo.SelectionLength)
                .Insert(campo.SelectionStart, entrada);

        private static void ValidarPegado(object sender, DataObjectPastingEventArgs e,
            Func<string, bool> validador)
        {
            if (sender is not TextBox campo ||
                !e.DataObject.GetDataPresent(DataFormats.Text) ||
                e.DataObject.GetData(DataFormats.Text) is not string texto ||
                !validador(ProyectarTexto(campo, texto)))
                e.CancelCommand();
        }

        private static bool EsEntero(string texto) =>
            texto.Length == 0 || texto.All(char.IsDigit);

        private static bool EsDecimal(string texto)
        {
            if (texto.Length == 0) return true;
            string separador = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (texto.Count(c => c.ToString() == separador) > 1) return false;
            return texto.All(c => char.IsDigit(c) || c.ToString() == separador);
        }
    }
}

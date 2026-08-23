using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartBins.Servicios
{
    /// <summary>
    /// Diálogo visual reutilizable para mantener mensajes y confirmaciones
    /// consistentes en toda la aplicación.
    /// </summary>
    public static class DialogoSmart
    {
        public static MessageBoxResult Mostrar(
            string mensaje,
            string titulo = "SmartBins",
            MessageBoxButton botones = MessageBoxButton.OK,
            MessageBoxImage tipo = MessageBoxImage.Information)
        {
            var ventana = CrearVentana(mensaje, titulo, botones, tipo);
            ventana.ShowDialog();
            return ventana.Tag is MessageBoxResult resultado
                ? resultado
                : MessageBoxResult.Cancel;
        }

        private static Window CrearVentana(
            string mensaje,
            string titulo,
            MessageBoxButton botones,
            MessageBoxImage tipo)
        {
            var ventana = new Window
            {
                Title = string.IsNullOrWhiteSpace(titulo) ? "SmartBins" : titulo,
                Width = 510,
                MinHeight = 230,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
                Background = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                Tag = botones is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel
                    ? MessageBoxResult.No
                    : MessageBoxResult.Cancel
            };

            Window? propietaria = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive && w != ventana)
                ?? Application.Current?.MainWindow;
            if (propietaria != null && propietaria.IsVisible)
                ventana.Owner = propietaria;

            var raiz = new Grid();
            raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var encabezado = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
                Padding = new Thickness(24, 17, 24, 17)
            };
            encabezado.Child = new TextBlock
            {
                Text = ventana.Title,
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold
            };
            raiz.Children.Add(encabezado);

            var contenido = new Grid { Margin = new Thickness(24, 24, 24, 20) };
            contenido.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            contenido.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var datosVisuales = ObtenerDatosVisuales(tipo);
            var insignia = new Border
            {
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(22),
                Background = datosVisuales.Fondo,
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = datosVisuales.Icono,
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = datosVisuales.Color,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            contenido.Children.Add(insignia);

            var texto = new TextBlock
            {
                Text = mensaje?.Trim() ?? string.Empty,
                FontSize = 14,
                LineHeight = 21,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(47, 58, 68)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(texto, 1);
            contenido.Children.Add(texto);
            Grid.SetRow(contenido, 1);
            raiz.Children.Add(contenido);

            var pie = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(246, 248, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 230, 234)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 14, 24, 14)
            };
            var acciones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            AgregarBotones(ventana, acciones, botones);
            pie.Child = acciones;
            Grid.SetRow(pie, 2);
            raiz.Children.Add(pie);

            ventana.Content = raiz;
            return ventana;
        }

        private static void AgregarBotones(
            Window ventana,
            Panel acciones,
            MessageBoxButton botones)
        {
            if (botones is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel)
            {
                acciones.Children.Add(CrearBoton(
                    ventana, "No", MessageBoxResult.No, false));
                if (botones == MessageBoxButton.YesNoCancel)
                    acciones.Children.Add(CrearBoton(
                        ventana, "Cancelar", MessageBoxResult.Cancel, false));
                acciones.Children.Add(CrearBoton(
                    ventana, "Sí, continuar", MessageBoxResult.Yes, true));
                return;
            }

            if (botones == MessageBoxButton.OKCancel)
                acciones.Children.Add(CrearBoton(
                    ventana, "Cancelar", MessageBoxResult.Cancel, false));

            acciones.Children.Add(CrearBoton(
                ventana, "Aceptar", MessageBoxResult.OK, true));
        }

        private static Button CrearBoton(
            Window ventana,
            string texto,
            MessageBoxResult resultado,
            bool principal)
        {
            var boton = new Button
            {
                Content = texto,
                MinWidth = 105,
                Height = 36,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(16, 0, 16, 0),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                IsDefault = principal,
                IsCancel = resultado is MessageBoxResult.Cancel or MessageBoxResult.No,
                BorderThickness = new Thickness(1)
            };
            boton.Style = Application.Current.TryFindResource(
                principal
                    ? "ProfessionalPrimaryButtonStyle"
                    : "ProfessionalSecondaryButtonStyle") as Style;
            boton.Click += (_, _) =>
            {
                ventana.Tag = resultado;
                ventana.Close();
            };
            return boton;
        }

        private static (string Icono, Brush Color, Brush Fondo) ObtenerDatosVisuales(
            MessageBoxImage tipo) =>
            tipo switch
            {
                MessageBoxImage.Error =>
                    ("×", new SolidColorBrush(Color.FromRgb(190, 40, 50)),
                     new SolidColorBrush(Color.FromRgb(253, 232, 234))),
                MessageBoxImage.Warning =>
                    ("!", new SolidColorBrush(Color.FromRgb(176, 100, 0)),
                     new SolidColorBrush(Color.FromRgb(255, 244, 218))),
                MessageBoxImage.Question =>
                    ("?", new SolidColorBrush(Color.FromRgb(0, 105, 145)),
                     new SolidColorBrush(Color.FromRgb(224, 243, 250))),
                _ =>
                    ("i", new SolidColorBrush(Color.FromRgb(0, 102, 204)),
                     new SolidColorBrush(Color.FromRgb(232, 243, 253)))
            };
    }
}

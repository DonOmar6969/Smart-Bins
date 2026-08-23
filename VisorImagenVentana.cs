using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartBins;

/// <summary>
/// Visor modal reutilizable para cualquier imagen de la aplicación.
/// </summary>
internal sealed class VisorImagenVentana : Window
{
    public VisorImagenVentana(ImageSource source)
    {
        Title = "Vista ampliada";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
        AllowsTransparency = false;
        ShowInTaskbar = false;
        Width = SystemParameters.WorkArea.Width * 0.9;
        Height = SystemParameters.WorkArea.Height * 0.9;
        MaxWidth = SystemParameters.WorkArea.Width;
        MaxHeight = SystemParameters.WorkArea.Height;

        var contenedor = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 39))
        };

        contenedor.Children.Add(new Image
        {
            Source = source,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(32)
        });

        var cerrar = new Button
        {
            Content = "✕",
            Width = 44,
            Height = 44,
            Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            FontSize = 21,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(210, 31, 41, 55)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "Cerrar"
        };
        cerrar.Click += (_, _) => Close();
        contenedor.Children.Add(cerrar);

        Content = contenedor;
        KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape)
            {
                Close();
            }
        };
    }
}

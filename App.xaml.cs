using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartBins
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Un único manejador cubre las imágenes actuales y las que se agreguen después.
            EventManager.RegisterClassHandler(
                typeof(Image),
                UIElement.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(AbrirImagenAmpliada));
        }

        private static void AbrirImagenAmpliada(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image { Source: not null } imagen ||
                imagen.ActualWidth <= 0 ||
                imagen.ActualHeight <= 0 ||
                Window.GetWindow(imagen) is VisorImagenVentana)
            {
                return;
            }

            var propietario = Window.GetWindow(imagen);
            var visor = new VisorImagenVentana(imagen.Source)
            {
                Owner = propietario
            };

            visor.ShowDialog();
        }
    }

}

namespace SmartBins.Servicios.Produccion
{
    public interface IStationInputService
    {
        event EventHandler? CompletarOperacionSolicitada;
        void SolicitarCompletarOperacion();
    }

    public class WpfStationInputService : IStationInputService
    {
        public event EventHandler? CompletarOperacionSolicitada;

        public void SolicitarCompletarOperacion()
            => CompletarOperacionSolicitada?.Invoke(this, EventArgs.Empty);
    }
}

namespace SmartBins.Modelos.Produccion
{
    public enum EstadoEstacion
    {
        SinUnidad,
        Lista,
        Trabajando,
        EsperandoConfirmacion,
        EsperandoTransferencia,
        Bloqueada,
        Pausada,
        ConError,
        Finalizada
    }
}

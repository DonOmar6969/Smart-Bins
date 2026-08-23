namespace SmartBins.Modelos.Produccion
{
    public sealed class ConfiguracionBalanceo
    {
        public int UmbralRepeticionesParaDistribuir { get; init; } = 10;
        public decimal ProporcionMezclaAlta { get; init; } = 0.50m;
        public int MinimoPartesDistintasParaMezclaAlta { get; init; } = 10;
        public int LongitudSufijoCorto { get; init; } = 2;
        public int LongitudSufijoLargo { get; init; } = 3;
        public int DiferenciasPermitidasSufijoLargo { get; init; } = 1;
    }
}

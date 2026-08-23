namespace SmartBins.Modelos
{
    public sealed class ConfiguracionAplicacion
    {
        public int OperadoresPredeterminados { get; set; } = 1;
        public int UnidadesPruebaPredeterminadas { get; set; } = 1;
        public List<int> NivelesTiempoCicloSegundos { get; set; } = new() { 2, 4, 6, 8, 10 };
        public int UmbralRepeticionesParaDistribuir { get; set; } = 10;
        public int ProporcionMezclaAltaPorcentaje { get; set; } = 50;
        public int MinimoPartesDistintasMezclaAlta { get; set; } = 10;
        public int LongitudSufijoExacto { get; set; } = 2;
        public int LongitudSufijoParecido { get; set; } = 3;
        public int DiferenciasPermitidasSufijo { get; set; } = 1;
    }
}

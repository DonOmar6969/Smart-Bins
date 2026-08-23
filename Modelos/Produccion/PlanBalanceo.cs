namespace SmartBins.Modelos.Produccion
{
    public class EstacionBalanceada
    {
        public int Numero { get; init; }
        public decimal CargaTotalSegundos { get; init; }
        public IReadOnlyList<OperacionPlanificada> Operaciones { get; init; } =
            Array.Empty<OperacionPlanificada>();
    }

    public class PlanBalanceo
    {
        public string EnsambleNombre { get; init; } = string.Empty;
        public int NumeroOperadores { get; init; }
        public decimal TiempoTotalSegundos { get; init; }
        public decimal TiempoObjetivoPorEstacion { get; init; }
        public decimal TiempoCicloLinea { get; init; }
        public IReadOnlyList<EstacionBalanceada> Estaciones { get; init; } =
            Array.Empty<EstacionBalanceada>();
        public bool EsMezclaAlta { get; init; }
        public IReadOnlyList<string> Recomendaciones { get; init; } =
            Array.Empty<string>();
    }
}

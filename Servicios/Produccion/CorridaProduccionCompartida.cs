
using SmartBins.Modelos.Produccion;// Importa los modelos del plan de balanceo y de las estaciones.


namespace SmartBins.Servicios.Produccion// Agrupa las clases encargadas de ejecutar y coordinar una corrida de producción.
{
    
    public class CorridaProduccionCompartida// Representa una sola corrida compartida por todas las estaciones de trabajo.
    {
        
        private readonly List<CorridaProduccionEnMemoria> estaciones;// Conserva las sesiones individuales que utilizará la ventana de cada operador.

        // Registra qué unidad ya fue liberada por cada estación.
        // La primera posición representa la estación y la segunda representa la unidad.
        private readonly bool[,] unidadesLiberadas;
        private bool finalizacionNotificada;

        
        public CorridaProduccionCompartida(PlanBalanceo plan, int cantidadUnidades)// Construye la corrida completa utilizando el plan calculado y la cantidad solicitada.
        {
            
            if (cantidadUnidades <= 0)// Evita iniciar una corrida sin unidades de producción.
               
                throw new ArgumentOutOfRangeException(nameof(cantidadUnidades)); // Informa cuál argumento contiene el valor inválido.


            if (plan.Estaciones.Count == 0)// Evita crear una corrida cuando el balanceo no produjo estaciones.
                throw new ArgumentException("El plan no contiene estaciones.", nameof(plan));
            Plan = plan;// Guarda el plan para que todas las estaciones consulten la misma información.

            // Guarda la cantidad total de unidades que atravesarán la línea.
            CantidadUnidades = cantidadUnidades;

            // Crea una casilla por cada combinación de estación y unidad.
            // Todas comienzan en false porque ninguna unidad ha sido liberada todavía.
            unidadesLiberadas = new bool[plan.Estaciones.Count, cantidadUnidades];

            // Toma las estaciones incluidas en el plan de balanceo.
            estaciones = plan.Estaciones
                // Las ordena para asegurar que la transferencia siga 1, 2, 3, etcétera.
                .OrderBy(e => e.Numero)
                // Crea una sesión de operador y le asigna su posición dentro de la línea.
                .Select((estacion, indice) =>
                    new CorridaProduccionEnMemoria(this, estacion, indice))
                // Materializa el resultado para conservar las mismas sesiones durante la corrida.
                .ToList();
        }

        // Expone el mismo plan de balanceo a todas las sesiones de operador.
        public PlanBalanceo Plan { get; }

        // Expone la cantidad total de unidades solicitadas por el supervisor.
        public int CantidadUnidades { get; }

        // Permite abrir una ventana por estación sin permitir reemplazar la lista internamente.
        public IReadOnlyList<CorridaProduccionEnMemoria> Estaciones => estaciones;

        public event EventHandler<ResultadoCorridaProduccion>? CorridaFinalizada;

        // Determina si una estación ya puede comenzar a procesar una unidad específica.
        internal bool PuedeProcesar(int indiceEstacion, int unidad)
            // La primera estación siempre recibe material directamente.
            // Las demás esperan a que la estación anterior libere la misma unidad.
            => indiceEstacion == 0 ||
               unidadesLiberadas[indiceEstacion - 1, unidad - 1];

        // Marca una unidad como terminada en una estación y avisa a la estación siguiente.
        internal void LiberarUnidad(int indiceEstacion, int unidad)
        {
            // Cambia a true la casilla correspondiente a la estación y unidad terminadas.
            unidadesLiberadas[indiceEstacion, unidad - 1] = true;

            // Comprueba que realmente exista una estación posterior.
            if (indiceEstacion + 1 < estaciones.Count)
                // Solicita que la siguiente estación revise si ya puede comenzar esa unidad.
                estaciones[indiceEstacion + 1].ActualizarDisponibilidad();
        }

        internal void NotificarEstacionFinalizada()
        {
            if (finalizacionNotificada ||
                estaciones.Any(e => e.Estado != EstadoEstacion.Finalizada))
                return;

            finalizacionNotificada = true;
            DateTime inicio = estaciones
                .Where(e => e.FechaInicioCorrida.HasValue)
                .Select(e => e.FechaInicioCorrida!.Value)
                .DefaultIfEmpty(DateTime.Now)
                .Min();
            DateTime fin = estaciones
                .Where(e => e.FechaFinCorrida.HasValue)
                .Select(e => e.FechaFinCorrida!.Value)
                .DefaultIfEmpty(DateTime.Now)
                .Max();

            CorridaFinalizada?.Invoke(this, new ResultadoCorridaProduccion
            {
                Ensamble = Plan.EnsambleNombre,
                CantidadUnidades = CantidadUnidades,
                FechaInicio = inicio,
                FechaFin = fin,
                OperacionesCompletadas = estaciones.Sum(e => e.OperacionesTotales),
                Estaciones = estaciones.Select(e => new ResultadoEstacionProduccion
                {
                    NumeroEstacion = e.NumeroEstacion,
                    OperacionesCompletadas = e.OperacionesTotales,
                    Duracion = e.FechaInicioCorrida.HasValue && e.FechaFinCorrida.HasValue
                        ? e.FechaFinCorrida.Value - e.FechaInicioCorrida.Value
                        : TimeSpan.Zero
                }).ToList()
            });
        }
    }

    public sealed class ResultadoCorridaProduccion : EventArgs
    {
        public string Ensamble { get; init; } = string.Empty;
        public int CantidadUnidades { get; init; }
        public int OperacionesCompletadas { get; init; }
        public DateTime FechaInicio { get; init; }
        public DateTime FechaFin { get; init; }
        public TimeSpan DuracionTotal => FechaFin - FechaInicio;
        public double TiempoCicloRealSegundos =>
            CantidadUnidades > 0 ? DuracionTotal.TotalSeconds / CantidadUnidades : 0;
        public double UnidadesPorHora =>
            DuracionTotal.TotalHours > 0 ? CantidadUnidades / DuracionTotal.TotalHours : 0;
        public List<ResultadoEstacionProduccion> Estaciones { get; init; } = new();
    }

    public sealed class ResultadoEstacionProduccion
    {
        public int NumeroEstacion { get; init; }
        public int OperacionesCompletadas { get; init; }
        public TimeSpan Duracion { get; init; }
    }
}

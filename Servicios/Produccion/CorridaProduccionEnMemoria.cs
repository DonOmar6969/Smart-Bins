// Importa los modelos que describen estaciones, operaciones, planes y estados.
using SmartBins.Modelos.Produccion;

// Agrupa la lógica utilizada durante una corrida de producción.
namespace SmartBins.Servicios.Produccion
{
    // Representa el estado y avance de una sola estación dentro de la corrida compartida.
    public class CorridaProduccionEnMemoria
    {
        // Mantiene la referencia al coordinador común para consultar y liberar unidades.
        private readonly CorridaProduccionCompartida corridaCompartida;

        // Guarda la posición de esta estación dentro del arreglo del coordinador.
        private readonly int indiceEstacion;

        // Conserva en orden las operaciones que debe ejecutar este operador.
        private readonly IReadOnlyList<OperacionPlanificada> operaciones;

        // Crea una sesión para una estación específica de la corrida compartida.
        internal CorridaProduccionEnMemoria(
            // Recibe el coordinador que conecta a todas las estaciones.
            CorridaProduccionCompartida corridaCompartida,
            // Recibe la distribución de operaciones asignada a esta estación.
            EstacionBalanceada estacion,
            // Recibe la posición interna usada para consultar la estación anterior.
            int indiceEstacion)
        {
            // Guarda el coordinador para poder consultar transferencias posteriormente.
            this.corridaCompartida = corridaCompartida;

            // Guarda el índice de esta estación dentro de la línea ordenada.
            this.indiceEstacion = indiceEstacion;

            // Utiliza el mismo plan que comparten todas las estaciones.
            Plan = corridaCompartida.Plan;

            // Guarda el número visible que se mostrará al operador.
            NumeroEstacion = estacion.Numero;

            // Utiliza la cantidad de unidades definida por el supervisor.
            CantidadUnidades = corridaCompartida.CantidadUnidades;

            // Conserva solamente las operaciones asignadas a esta estación.
            operaciones = estacion.Operaciones;

            // Una estación vacía termina inmediatamente.
            // La primera estación queda lista y las posteriores esperan una transferencia.
            Estado = operaciones.Count == 0
                ? EstadoEstacion.Finalizada
                : indiceEstacion == 0
                    ? EstadoEstacion.Lista
                    : EstadoEstacion.EsperandoTransferencia;
        }

        // Notifica a la ventana cuando cambia el estado o la operación que debe mostrar.
        public event EventHandler? EstadoCambiado;

        // Expone el plan de balanceo relacionado con esta corrida.
        public PlanBalanceo Plan { get; }

        // Expone el número visible de la estación.
        public int NumeroEstacion { get; }

        // Expone el total de unidades que procesará la línea.
        public int CantidadUnidades { get; }

        // Calcula cuántas operaciones ejecuta esta estación por cada unidad.
        public int OperacionesPorUnidad => operaciones.Count;

        // Calcula el trabajo total multiplicando operaciones por unidades.
        public int OperacionesTotales => operaciones.Count * CantidadUnidades;

        // Calcula cuántas operaciones han sido completadas hasta el momento.
        public int OperacionesCompletadas =>
            // Suma todas las unidades anteriores y las operaciones avanzadas en la unidad actual.
            ((UnidadActual - 1) * operaciones.Count) + IndiceOperacionActual;

        // Indica qué unidad está atendiendo o esperando actualmente la estación.
        public int UnidadActual { get; private set; } = 1;

        // Indica la posición de la operación actual dentro de la estación.
        public int IndiceOperacionActual { get; private set; }

        // Indica si la estación espera, está lista, trabaja o ya terminó.
        public EstadoEstacion Estado { get; private set; }

        // Registra cuándo comenzó por primera vez esta estación.
        public DateTime? FechaInicioCorrida { get; private set; }

        // Registra cuándo terminó todas sus unidades esta estación.
        public DateTime? FechaFinCorrida { get; private set; }

        // Registra cuándo comenzó la operación que está actualmente en pantalla.
        public DateTime? FechaInicioOperacion { get; private set; }

        // Calcula en tiempo real cuánto lleva abierta la operación actual.
        public TimeSpan TiempoTranscurridoOperacion =>
            // Si existe una hora de inicio, resta esa hora al momento actual.
            FechaInicioOperacion.HasValue
                ? DateTime.Now - FechaInicioOperacion.Value
                // Si todavía no comienza, devuelve una duración de cero.
                : TimeSpan.Zero;

        // Devuelve la operación actual o null cuando ya no quedan operaciones.
        public OperacionPlanificada? OperacionActual =>
            IndiceOperacionActual < operaciones.Count ? operaciones[IndiceOperacionActual] : null;

        // Comienza a medir el tiempo de la operación mostrada al operador.
        public void IniciarOperacion()
        {
            // Solo permite iniciar cuando la estación tiene una unidad disponible.
            if (Estado != EstadoEstacion.Lista)
                // Sale sin modificar el estado si todavía está esperando o ya trabaja.
                return;

            // Registra el inicio general únicamente la primera vez que trabaja la estación.
            FechaInicioCorrida ??= DateTime.Now;

            // Registra el inicio independiente de esta operación.
            FechaInicioOperacion = DateTime.Now;

            // Cambia el estado para permitir que la operación pueda completarse.
            Estado = EstadoEstacion.Trabajando;
        }

        // Completa la operación actual y decide cuál es el siguiente paso.
        public bool CompletarOperacion()
        {
            // Impide completar una operación que no se encuentra trabajando.
            if (Estado != EstadoEstacion.Trabajando)
                // Informa que no se realizó ningún cambio.
                return false;

            // Avanza a la siguiente posición dentro de las operaciones de la estación.
            IndiceOperacionActual++;

            // Detiene el cálculo de tiempo de la operación terminada.
            FechaInicioOperacion = null;

            // Comprueba si todavía quedan operaciones para esta misma unidad.
            if (IndiceOperacionActual < operaciones.Count)
            {
                // Deja lista la siguiente operación de la unidad actual.
                Estado = EstadoEstacion.Lista;

                // Solicita a la ventana que muestre la siguiente operación.
                NotificarCambio();

                // Informa que la operación se completó correctamente.
                return true;
            }

            // Conserva el número de la unidad que acaba de completar todas sus operaciones.
            int unidadTerminada = UnidadActual;

            // Libera esa unidad para que pueda recibirla la estación siguiente.
            corridaCompartida.LiberarUnidad(indiceEstacion, unidadTerminada);

            // Comprueba si esta estación todavía tiene más unidades pendientes.
            if (UnidadActual < CantidadUnidades)
            {
                // Avanza a la siguiente unidad que debe procesar esta estación.
                UnidadActual++;

                // Regresa al primer paso de las operaciones asignadas.
                IndiceOperacionActual = 0;

                // La primera estación normalmente puede continuar de inmediato.
                // Las demás esperan si la estación anterior aún no libera la siguiente unidad.
                Estado = corridaCompartida.PuedeProcesar(indiceEstacion, UnidadActual)
                    ? EstadoEstacion.Lista
                    : EstadoEstacion.EsperandoTransferencia;

                // Actualiza la ventana con la siguiente unidad o el mensaje de espera.
                NotificarCambio();

                // Informa que la última operación de la unidad se completó correctamente.
                return true;
            }

            // Marca la estación como finalizada cuando ya procesó todas sus unidades.
            Estado = EstadoEstacion.Finalizada;

            // Guarda la hora exacta en la que terminó esta estación.
            FechaFinCorrida = DateTime.Now;

            corridaCompartida.NotificarEstacionFinalizada();

            // Solicita a la ventana que presente el mensaje final.
            NotificarCambio();

            // Informa que la operación final se completó correctamente.
            return true;
        }

        // Revisa si una estación que esperaba ya recibió autorización para trabajar.
        internal void ActualizarDisponibilidad()
        {
            // Solo continúa si estaba esperando y la estación anterior liberó esta unidad.
            if (Estado != EstadoEstacion.EsperandoTransferencia ||
                !corridaCompartida.PuedeProcesar(indiceEstacion, UnidadActual))
                // No modifica nada si la unidad aún no está disponible.
                return;

            // Cambia a lista para que la ventana pueda iniciar la primera operación.
            Estado = EstadoEstacion.Lista;

            // Avisa a la ventana que debe reemplazar el mensaje de espera.
            NotificarCambio();
        }

        // Centraliza el envío de notificaciones hacia la interfaz del operador.
        private void NotificarCambio()
            // Ejecuta el evento únicamente si alguna ventana se encuentra suscrita.
            => EstadoCambiado?.Invoke(this, EventArgs.Empty);
    }
}

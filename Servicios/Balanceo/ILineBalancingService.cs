using SmartBins.Modelos;
using SmartBins.Modelos.Produccion;

namespace SmartBins.Servicios.Balanceo
{
    public interface ILineBalancingService
    {
        PlanBalanceo Calcular( // PlanBalanceo es un objeto que contiene la información del plan de balanceo calculado
            string ensambleNombre, // Nombre del ensamble para el cual se está calculando el balanceo
            IEnumerable<SecuenciaEnsamble> secuencia, // Secuencia de operaciones del ensamble
            IEnumerable<Componente> componentes, // Lista de componentes disponibles para el ensamble
            int numeroOperadores, // Número de operadores disponibles para el ensamble
            ConfiguracionBalanceo? configuracion = null);
    }
}

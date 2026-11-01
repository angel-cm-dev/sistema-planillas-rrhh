using Planillas.Domain.Common;

namespace Planillas.Domain.Entities;

/// <summary>
/// Parámetros legales usados por el cálculo de planilla. Se guardan en BD (una sola fila)
/// para poder actualizarlos sin recompilar cuando cambian la UIT, la RMV o las tasas.
/// TODOS los valores del seed son REFERENCIALES.
/// </summary>
public class ParametrosPlanilla : Entity
{
    public decimal Uit { get; set; }
    /// <summary>Remuneración Mínima Vital.</summary>
    public decimal Rmv { get; set; }
    public decimal AsignacionFamiliarPct { get; set; }
    public decimal OnpPct { get; set; }
    public decimal EsSaludPct { get; set; }
    public decimal HoraExtra25Pct { get; set; }
    public decimal HoraExtra35Pct { get; set; }
    /// <summary>Remuneración máxima asegurable: tope sobre el que se calcula la prima de seguro AFP.</summary>
    public decimal TopePrimaAfp { get; set; }
    /// <summary>Horas de la jornada diaria ordinaria (para obtener el valor hora).</summary>
    public decimal HorasJornadaDiaria { get; set; }
}

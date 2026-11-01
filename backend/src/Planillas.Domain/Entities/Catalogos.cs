using Planillas.Domain.Common;

namespace Planillas.Domain.Entities;

public class Area : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}

public class Cargo : Entity
{
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// Administradora de Fondos de Pensiones con sus tasas. Son configurables porque la SBS
/// las actualiza periódicamente; los valores del seed son REFERENCIALES.
/// </summary>
public class Afp : Entity
{
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Aporte obligatorio al fondo (porcentaje: 10 = 10%).</summary>
    public decimal AportePct { get; set; }
    /// <summary>Prima de seguro de invalidez y sobrevivencia (aplica hasta la remuneración máxima asegurable).</summary>
    public decimal PrimaSeguroPct { get; set; }
    /// <summary>Comisión sobre el flujo (sobre la remuneración).</summary>
    public decimal ComisionPct { get; set; }
}

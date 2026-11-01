using Planillas.Domain.Common;
using Planillas.Domain.Enums;

namespace Planillas.Domain.Entities;

public class Planilla : Entity
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public EstadoPlanilla Estado { get; set; } = EstadoPlanilla.Borrador;
    public DateTime? FechaCierre { get; set; }
    public ICollection<BoletaPago> Boletas { get; set; } = new List<BoletaPago>();

    public string Periodo => $"{Anio}-{Mes:00}";

    public void AsegurarEditable()
    {
        if (Estado == EstadoPlanilla.Cerrada)
            throw new DomainException($"La planilla {Periodo} está cerrada y no puede modificarse.");
    }

    public void Cerrar()
    {
        AsegurarEditable();
        if (Boletas.Count == 0)
            throw new DomainException("No se puede cerrar una planilla sin boletas.");
        Estado = EstadoPlanilla.Cerrada;
        FechaCierre = DateTime.UtcNow;
    }
}

/// <summary>
/// Boleta de pago. Guarda una FOTO de los montos y datos del empleado al momento del cálculo:
/// si luego cambia el sueldo o la AFP, la boleta histórica no debe alterarse.
/// </summary>
public class BoletaPago : Entity
{
    public int PlanillaId { get; set; }
    public Planilla? Planilla { get; set; }
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    // Snapshot de datos del trabajador
    public string EmpleadoNombre { get; set; } = string.Empty;
    public string EmpleadoDni { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public RegimenPensionario Regimen { get; set; }
    public string? AfpNombre { get; set; }

    // Ingresos
    public decimal SueldoBasico { get; set; }
    public decimal AsignacionFamiliar { get; set; }
    public decimal HorasExtra25 { get; set; }
    public decimal HorasExtra35 { get; set; }
    public decimal MontoHorasExtra { get; set; }
    public int DiasFalta { get; set; }
    public decimal DescuentoFaltas { get; set; }
    public decimal TotalIngresos { get; set; }

    // Descuentos al trabajador
    public decimal Onp { get; set; }
    public decimal AfpAporte { get; set; }
    public decimal AfpPrima { get; set; }
    public decimal AfpComision { get; set; }
    public decimal Renta5ta { get; set; }
    public decimal TotalDescuentos { get; set; }

    public decimal NetoPagar { get; set; }

    // Aporte del empleador (NO se descuenta al trabajador)
    public decimal EsSalud { get; set; }
}

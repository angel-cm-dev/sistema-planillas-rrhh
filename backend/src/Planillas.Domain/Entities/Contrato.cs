using Planillas.Domain.Common;
using Planillas.Domain.Enums;

namespace Planillas.Domain.Entities;

public class Contrato : Entity
{
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public TipoContrato Tipo { get; set; }
    public DateOnly FechaInicio { get; set; }
    /// <summary>Nulo para contratos a plazo indeterminado.</summary>
    public DateOnly? FechaFin { get; set; }
    public decimal Sueldo { get; set; }
    public EstadoContrato Estado { get; set; } = EstadoContrato.Vigente;

    public void Validar()
    {
        if (Tipo == TipoContrato.PlazoFijo && FechaFin is null)
            throw new DomainException("Un contrato a plazo fijo requiere fecha de fin.");
        if (FechaFin is not null && FechaFin < FechaInicio)
            throw new DomainException("La fecha de fin no puede ser anterior a la fecha de inicio.");
    }

    public void Finalizar(DateOnly fecha)
    {
        if (Estado == EstadoContrato.Finalizado)
            throw new DomainException("El contrato ya está finalizado.");
        if (fecha < FechaInicio)
            throw new DomainException("La fecha de cese no puede ser anterior al inicio del contrato.");
        Estado = EstadoContrato.Finalizado;
        FechaFin = fecha;
    }
}

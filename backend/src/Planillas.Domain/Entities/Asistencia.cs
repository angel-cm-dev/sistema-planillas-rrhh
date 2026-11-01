using Planillas.Domain.Common;
using Planillas.Domain.Enums;

namespace Planillas.Domain.Entities;

/// <summary>Registro diario de asistencia. Es la fuente de faltas y horas extra para la planilla.</summary>
public class Asistencia : Entity
{
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public DateOnly Fecha { get; set; }
    public EstadoAsistencia Estado { get; set; }
    public TimeOnly? HoraEntrada { get; set; }
    public TimeOnly? HoraSalida { get; set; }
    /// <summary>
    /// Horas extra del día. Las 2 primeras se pagan con 25% de sobretasa y las siguientes con 35%
    /// (D.S. 007-2002-TR). Por eso se registran por día y no como total mensual.
    /// </summary>
    public decimal HorasExtra { get; set; }
    public string? Observacion { get; set; }

    public void Validar()
    {
        if (HorasExtra < 0 || HorasExtra > 12)
            throw new DomainException("Las horas extra deben estar entre 0 y 12 por día.");
        if (Estado == EstadoAsistencia.Falta && HorasExtra > 0)
            throw new DomainException("No se pueden registrar horas extra en un día de falta.");
        if (HoraEntrada is not null && HoraSalida is not null && HoraSalida <= HoraEntrada)
            throw new DomainException("La hora de salida debe ser posterior a la de entrada.");
    }
}

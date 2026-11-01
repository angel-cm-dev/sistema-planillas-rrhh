using Planillas.Domain.Common;
using Planillas.Domain.Enums;

namespace Planillas.Domain.Entities;

public class Empleado : Entity
{
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly FechaIngreso { get; set; }

    public int AreaId { get; set; }
    public Area? Area { get; set; }
    public int CargoId { get; set; }
    public Cargo? Cargo { get; set; }

    public decimal SueldoBasico { get; set; }

    /// <summary>
    /// Ley 25129: el trabajador con hijos menores de 18 años (o cursando estudios superiores)
    /// percibe asignación familiar equivalente al 10% de la RMV.
    /// </summary>
    public bool TieneHijos { get; set; }

    public RegimenPensionario Regimen { get; set; }
    public int? AfpId { get; set; }
    public Afp? Afp { get; set; }
    /// <summary>Código Único del Sistema Privado de Pensiones (solo AFP).</summary>
    public string? Cuspp { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    /// <summary>Un afiliado AFP necesita una AFP asignada; uno ONP no debe tenerla.</summary>
    public void NormalizarRegimen()
    {
        if (Regimen == RegimenPensionario.Afp && AfpId is null)
            throw new DomainException("Un empleado en régimen AFP debe tener una AFP asignada.");
        if (Regimen == RegimenPensionario.Onp)
        {
            AfpId = null;
            Cuspp = null;
        }
    }

    /// <summary>
    /// Saldo vacacional simplificado: 30 días por cada año completo de servicios menos los días
    /// ya tomados o comprometidos. (La ley exige cumplir el "récord vacacional" de un año.)
    /// </summary>
    public int DiasVacacionesGanados(DateOnly hoy)
    {
        var anios = hoy.Year - FechaIngreso.Year;
        if (FechaIngreso.AddYears(anios) > hoy) anios--;
        return Math.Max(0, anios) * SolicitudVacaciones.DiasPorAnio;
    }
}

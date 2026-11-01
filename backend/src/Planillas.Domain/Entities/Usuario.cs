using Planillas.Domain.Common;
using Planillas.Domain.Enums;

namespace Planillas.Domain.Entities;

public class Usuario : Entity
{
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Rol Rol { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Solo los usuarios con rol Empleado se vinculan a una ficha de empleado (para ver sus boletas).</summary>
    public int? EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
}

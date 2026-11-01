using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Planillas.Application.Common;
using Planillas.Domain.Enums;
using Planillas.Infrastructure.Security;

namespace Planillas.Api.Infrastructure;

/// <summary>Adaptador que expone los claims del JWT a la capa de aplicación sin acoplarla a HTTP.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public int UserId => int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public Rol Rol => Enum.TryParse<Rol>(User.FindFirstValue(ClaimTypes.Role), out var rol) ? rol : Rol.Empleado;

    public int? EmpleadoId => int.TryParse(User.FindFirstValue(AppClaims.EmpleadoId), out var id) ? id : null;
}

/// <summary>Nombres de roles y políticas, para no repetir strings mágicos en los controladores.</summary>
public static class Roles
{
    public const string Admin = nameof(Rol.Admin);
    public const string Rrhh = nameof(Rol.Rrhh);
    public const string Empleado = nameof(Rol.Empleado);
    /// <summary>Admin o RR.HH.: gestionan información de todos los trabajadores.</summary>
    public const string Gestores = Admin + "," + Rrhh;
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Planillas.Application.Common;
using Planillas.Domain.Entities;

namespace Planillas.Infrastructure.Security;

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "planillas-rrhh";
    public string Audience { get; set; } = "planillas-rrhh-web";
    /// <summary>Clave simétrica HMAC-SHA256: mínimo 32 caracteres. Se inyecta por variable de entorno en producción.</summary>
    public string Key { get; set; } = string.Empty;
    public int ExpiraEnMinutos { get; set; } = 480;
}

/// <summary>Claims personalizados usados por la API.</summary>
public static class AppClaims
{
    public const string EmpleadoId = "empleado_id";
}

/// <summary>
/// BCrypt con factor de trabajo 11: lento a propósito (≈100 ms) para encarecer ataques de fuerza
/// bruta, e incluye sal aleatoria por contraseña dentro del propio hash.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public (string Token, DateTime ExpiraEn) CrearToken(Usuario usuario)
    {
        var o = options.Value;
        var expira = DateTime.UtcNow.AddMinutes(o.ExpiraEnMinutos);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Name, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (usuario.EmpleadoId is not null)
            claims.Add(new Claim(AppClaims.EmpleadoId, usuario.EmpleadoId.Value.ToString()));

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(o.Issuer, o.Audience, claims, expires: expira, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

/// <summary>"Hoy" en hora de Lima: el periodo de planilla y el saldo vacacional dependen de la fecha local.</summary>
public class LimaClock : IClock
{
    private static readonly TimeZoneInfo Zona = ObtenerZona();

    public DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona));

    private static TimeZoneInfo ObtenerZona()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("Lima", TimeSpan.FromHours(-5), "Lima", "Lima"); }
    }
}

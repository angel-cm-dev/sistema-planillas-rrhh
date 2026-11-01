using Microsoft.EntityFrameworkCore;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;

namespace Planillas.Application.Common;

/// <summary>
/// Puerto hacia la persistencia. La capa de aplicación depende de esta abstracción y no del
/// DbContext concreto; así las pruebas pueden usar SQLite y producción SQL Server (ver ADR-0002).
/// </summary>
public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Area> Areas { get; }
    DbSet<Cargo> Cargos { get; }
    DbSet<Afp> Afps { get; }
    DbSet<Empleado> Empleados { get; }
    DbSet<Contrato> Contratos { get; }
    DbSet<Asistencia> Asistencias { get; }
    DbSet<SolicitudVacaciones> SolicitudesVacaciones { get; }
    DbSet<ParametrosPlanilla> Parametros { get; }
    DbSet<Planilla> Planillas { get; }
    DbSet<BoletaPago> Boletas { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Usuario autenticado en la petición actual (implementado en la capa API a partir del JWT).</summary>
public interface ICurrentUser
{
    int UserId { get; }
    Rol Rol { get; }
    int? EmpleadoId { get; }
    bool EsGestor => Rol is Rol.Admin or Rol.Rrhh;
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiraEn) CrearToken(Usuario usuario);
}

/// <summary>Reloj inyectable: permite fijar "hoy" en pruebas (saldo vacacional, periodos).</summary>
public interface IClock
{
    DateOnly Hoy { get; }
}

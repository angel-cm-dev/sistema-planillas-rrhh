using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;
using Planillas.Domain.Entities;

namespace Planillas.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Afp> Afps => Set<Afp>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();
    public DbSet<SolicitudVacaciones> SolicitudesVacaciones => Set<SolicitudVacaciones>();
    public DbSet<ParametrosPlanilla> Parametros => Set<ParametrosPlanilla>();
    public DbSet<Planilla> Planillas => Set<Planilla>();
    public DbSet<BoletaPago> Boletas => Set<BoletaPago>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Montos con 2 decimales por defecto (soles y céntimos).
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);

        // SQLite no tiene tipo decimal nativo y no permite ORDER BY / SUM sobre decimales guardados
        // como texto. Solo en SQLite (pruebas y desarrollo local) los convertimos a double.
        if (Database.IsSqlite())
            configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }
}

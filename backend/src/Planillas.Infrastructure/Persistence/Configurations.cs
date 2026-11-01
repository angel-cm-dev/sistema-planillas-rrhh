using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planillas.Domain.Entities;

namespace Planillas.Infrastructure.Persistence;

// Configuración Fluent API: el dominio queda libre de atributos de persistencia.
// Los enums se guardan como texto (más legible en reportes SQL y robusto ante reordenamientos).

internal class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.Property(x => x.Email).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.NombreCompleto).HasMaxLength(150).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal class AreaConfig : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Nombre).IsUnique();
    }
}

internal class CargoConfig : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Nombre).IsUnique();
    }
}

internal class AfpConfig : IEntityTypeConfiguration<Afp>
{
    public void Configure(EntityTypeBuilder<Afp> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Nombre).IsUnique();
        // Las tasas necesitan más decimales que los montos (p. ej. 1.37%).
        b.Property(x => x.AportePct).HasPrecision(7, 4);
        b.Property(x => x.PrimaSeguroPct).HasPrecision(7, 4);
        b.Property(x => x.ComisionPct).HasPrecision(7, 4);
    }
}

internal class EmpleadoConfig : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> b)
    {
        b.Property(x => x.Dni).HasMaxLength(8).IsFixedLength().IsRequired();
        b.HasIndex(x => x.Dni).IsUnique();
        b.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(150).IsRequired();
        b.Property(x => x.Cuspp).HasMaxLength(12);
        b.Property(x => x.Regimen).HasConversion<string>().HasMaxLength(10);
        b.Ignore(x => x.NombreCompleto);
        b.HasOne(x => x.Area).WithMany(a => a.Empleados).HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Cargo).WithMany().HasForeignKey(x => x.CargoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Afp).WithMany().HasForeignKey(x => x.AfpId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Activo, x.AreaId });
    }
}

internal class ContratoConfig : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> b)
    {
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Empleado).WithMany(e => e.Contratos).HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal class AsistenciaConfig : IEntityTypeConfiguration<Asistencia>
{
    public void Configure(EntityTypeBuilder<Asistencia> b)
    {
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.HorasExtra).HasPrecision(4, 2);
        b.Property(x => x.Observacion).HasMaxLength(250);
        // Un único registro por empleado y día: lo garantiza la BD, no solo el código.
        b.HasIndex(x => new { x.EmpleadoId, x.Fecha }).IsUnique();
        b.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal class SolicitudVacacionesConfig : IEntityTypeConfiguration<SolicitudVacaciones>
{
    public void Configure(EntityTypeBuilder<SolicitudVacaciones> b)
    {
        b.ToTable("SolicitudesVacaciones");
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Motivo).HasMaxLength(250);
        b.Property(x => x.ComentarioRevision).HasMaxLength(250);
        b.Ignore(x => x.Dias);
        b.Ignore(x => x.ConsumeSaldo);
        b.HasIndex(x => new { x.EmpleadoId, x.Estado });
        b.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal class ParametrosConfig : IEntityTypeConfiguration<ParametrosPlanilla>
{
    public void Configure(EntityTypeBuilder<ParametrosPlanilla> b)
    {
        b.ToTable("ParametrosPlanilla");
        b.Property(x => x.AsignacionFamiliarPct).HasPrecision(7, 4);
        b.Property(x => x.OnpPct).HasPrecision(7, 4);
        b.Property(x => x.EsSaludPct).HasPrecision(7, 4);
        b.Property(x => x.HoraExtra25Pct).HasPrecision(7, 4);
        b.Property(x => x.HoraExtra35Pct).HasPrecision(7, 4);
        b.Property(x => x.HorasJornadaDiaria).HasPrecision(4, 2);
    }
}

internal class PlanillaConfig : IEntityTypeConfiguration<Planilla>
{
    public void Configure(EntityTypeBuilder<Planilla> b)
    {
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.Periodo);
        // Una planilla por periodo.
        b.HasIndex(x => new { x.Anio, x.Mes }).IsUnique();
    }
}

internal class BoletaConfig : IEntityTypeConfiguration<BoletaPago>
{
    public void Configure(EntityTypeBuilder<BoletaPago> b)
    {
        b.ToTable("BoletasPago");
        b.Property(x => x.EmpleadoNombre).HasMaxLength(200);
        b.Property(x => x.EmpleadoDni).HasMaxLength(8);
        b.Property(x => x.Cargo).HasMaxLength(100);
        b.Property(x => x.Area).HasMaxLength(100);
        b.Property(x => x.AfpNombre).HasMaxLength(50);
        b.Property(x => x.Regimen).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.HorasExtra25).HasPrecision(6, 2);
        b.Property(x => x.HorasExtra35).HasPrecision(6, 2);
        b.HasOne(x => x.Planilla).WithMany(p => p.Boletas).HasForeignKey(x => x.PlanillaId).OnDelete(DeleteBehavior.Cascade);
        // Restrict: no se puede borrar un empleado que tiene boletas (documentos laborales).
        b.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlanillaId, x.EmpleadoId }).IsUnique();
    }
}

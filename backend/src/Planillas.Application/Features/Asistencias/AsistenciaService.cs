using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;

namespace Planillas.Application.Features.Asistencias;

public class AsistenciaQuery : PageQuery
{
    public int? EmpleadoId { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public EstadoAsistencia? Estado { get; set; }
}

public record AsistenciaDto(
    int Id, int EmpleadoId, string Empleado, DateOnly Fecha, string Estado,
    TimeOnly? HoraEntrada, TimeOnly? HoraSalida, decimal HorasExtra, string? Observacion);

public record AsistenciaRequest(
    int EmpleadoId, DateOnly Fecha, EstadoAsistencia Estado, TimeOnly? HoraEntrada, TimeOnly? HoraSalida,
    decimal HorasExtra, string? Observacion);

public class AsistenciaRequestValidator : AbstractValidator<AsistenciaRequest>
{
    public AsistenciaRequestValidator()
    {
        RuleFor(x => x.EmpleadoId).GreaterThan(0);
        RuleFor(x => x.Estado).IsInEnum();
        RuleFor(x => x.HorasExtra).InclusiveBetween(0, 12);
        RuleFor(x => x.Observacion).MaximumLength(250);
    }
}

public class AsistenciaService(IAppDbContext db, ICurrentUser currentUser, IClock clock, IValidator<AsistenciaRequest> validator)
{
    public async Task<PagedResult<AsistenciaDto>> ListAsync(AsistenciaQuery q, CancellationToken ct)
    {
        var query = db.Asistencias.AsNoTracking().AsQueryable();

        // Un empleado solo ve su propia asistencia, aunque envíe otro empleadoId.
        if (!currentUser.EsGestor)
            q.EmpleadoId = currentUser.EmpleadoId ?? throw new ForbiddenException("Su usuario no está vinculado a un empleado.");

        if (q.EmpleadoId is not null) query = query.Where(a => a.EmpleadoId == q.EmpleadoId);
        if (q.Desde is not null) query = query.Where(a => a.Fecha >= q.Desde);
        if (q.Hasta is not null) query = query.Where(a => a.Fecha <= q.Hasta);
        if (q.Estado is not null) query = query.Where(a => a.Estado == q.Estado);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(a => a.Empleado!.Nombres.ToLower().Contains(s) || a.Empleado.Apellidos.ToLower().Contains(s));
        }

        query = q.Descendente || q.SortDir is null
            ? query.OrderByDescending(a => a.Fecha).ThenBy(a => a.Empleado!.Apellidos)
            : query.OrderBy(a => a.Fecha).ThenBy(a => a.Empleado!.Apellidos);

        return await query
            .Select(a => new AsistenciaDto(a.Id, a.EmpleadoId, a.Empleado!.Nombres + " " + a.Empleado.Apellidos, a.Fecha,
                a.Estado.ToString(), a.HoraEntrada, a.HoraSalida, a.HorasExtra, a.Observacion))
            .ToPagedResultAsync(q, ct);
    }

    /// <summary>
    /// Registra (o corrige) la asistencia de un día. Hay como máximo un registro por empleado y fecha,
    /// por eso este método actúa como "upsert" en lugar de duplicar filas.
    /// </summary>
    public async Task<AsistenciaDto> RegistrarAsync(AsistenciaRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        if (r.Fecha > clock.Hoy)
            throw new Domain.Common.DomainException("No se puede registrar asistencia en una fecha futura.");
        var empleado = await db.Empleados.FirstOrDefaultAsync(e => e.Id == r.EmpleadoId, ct)
            ?? throw new NotFoundException("Empleado", r.EmpleadoId);

        // Si la planilla del periodo ya está cerrada, la asistencia de ese mes queda congelada.
        if (await db.Planillas.AnyAsync(p => p.Anio == r.Fecha.Year && p.Mes == r.Fecha.Month && p.Estado == EstadoPlanilla.Cerrada, ct))
            throw new ConflictException($"La planilla {r.Fecha.Year}-{r.Fecha.Month:00} está cerrada; no se puede modificar su asistencia.");

        var asistencia = await db.Asistencias.FirstOrDefaultAsync(a => a.EmpleadoId == r.EmpleadoId && a.Fecha == r.Fecha, ct);
        if (asistencia is null)
        {
            asistencia = new Asistencia { EmpleadoId = r.EmpleadoId, Fecha = r.Fecha };
            db.Asistencias.Add(asistencia);
        }
        asistencia.Estado = r.Estado;
        asistencia.HoraEntrada = r.HoraEntrada;
        asistencia.HoraSalida = r.HoraSalida;
        asistencia.HorasExtra = r.HorasExtra;
        asistencia.Observacion = r.Observacion;
        asistencia.Validar();

        await db.SaveChangesAsync(ct);
        return new AsistenciaDto(asistencia.Id, empleado.Id, empleado.NombreCompleto, asistencia.Fecha, asistencia.Estado.ToString(),
            asistencia.HoraEntrada, asistencia.HoraSalida, asistencia.HorasExtra, asistencia.Observacion);
    }
}

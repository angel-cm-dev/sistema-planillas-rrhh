using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;
using Planillas.Domain.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;

namespace Planillas.Application.Features.Vacaciones;

public class VacacionQuery : PageQuery
{
    public int? EmpleadoId { get; set; }
    public EstadoSolicitud? Estado { get; set; }
}

public record SolicitudDto(
    int Id, int EmpleadoId, string Empleado, DateOnly FechaInicio, DateOnly FechaFin, int Dias,
    string Estado, string? Motivo, string? ComentarioRevision, DateTime? FechaRevision, DateTime CreadoEn);

/// <summary>EmpleadoId es opcional: si lo omite un empleado, se usa su propia ficha.</summary>
public record SolicitudRequest(int? EmpleadoId, DateOnly FechaInicio, DateOnly FechaFin, string? Motivo);
public record RevisionRequest(string? Comentario);
public record SaldoVacacionesDto(int EmpleadoId, int DiasGanados, int DiasConsumidos, int DiasDisponibles);

public class SolicitudRequestValidator : AbstractValidator<SolicitudRequest>
{
    public SolicitudRequestValidator()
    {
        RuleFor(x => x.FechaFin).GreaterThanOrEqualTo(x => x.FechaInicio)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
        RuleFor(x => x.Motivo).MaximumLength(250);
    }
}

public class VacacionService(IAppDbContext db, ICurrentUser currentUser, IClock clock, IValidator<SolicitudRequest> validator)
{
    public async Task<PagedResult<SolicitudDto>> ListAsync(VacacionQuery q, CancellationToken ct)
    {
        var query = db.SolicitudesVacaciones.AsNoTracking().AsQueryable();
        if (!currentUser.EsGestor)
            q.EmpleadoId = currentUser.EmpleadoId ?? throw new ForbiddenException("Su usuario no está vinculado a un empleado.");
        if (q.EmpleadoId is not null) query = query.Where(s => s.EmpleadoId == q.EmpleadoId);
        if (q.Estado is not null) query = query.Where(s => s.Estado == q.Estado);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(x => x.Empleado!.Nombres.ToLower().Contains(s) || x.Empleado.Apellidos.ToLower().Contains(s));
        }

        return await query.OrderByDescending(s => s.CreadoEn)
            .Select(s => new SolicitudDto(s.Id, s.EmpleadoId, s.Empleado!.Nombres + " " + s.Empleado.Apellidos,
                s.FechaInicio, s.FechaFin, s.FechaFin.DayNumber - s.FechaInicio.DayNumber + 1, s.Estado.ToString(),
                s.Motivo, s.ComentarioRevision, s.FechaRevision, s.CreadoEn))
            .ToPagedResultAsync(q, ct);
    }

    public async Task<SaldoVacacionesDto> SaldoAsync(int? empleadoId, CancellationToken ct)
    {
        var id = ResolverEmpleado(empleadoId);
        var empleado = await db.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Empleado", id);
        return await CalcularSaldo(empleado, ct);
    }

    public async Task<SolicitudDto> CrearAsync(SolicitudRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        var empleadoId = ResolverEmpleado(r.EmpleadoId);
        var empleado = await db.Empleados.FirstOrDefaultAsync(e => e.Id == empleadoId, ct)
            ?? throw new NotFoundException("Empleado", empleadoId);
        if (!empleado.Activo) throw new DomainException("El empleado está inactivo.");
        if (r.FechaInicio < clock.Hoy) throw new DomainException("No se pueden solicitar vacaciones en fechas pasadas.");

        var vivas = await db.SolicitudesVacaciones
            .Where(s => s.EmpleadoId == empleadoId && (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada))
            .ToListAsync(ct);
        if (vivas.Any(s => s.SeSolapaCon(r.FechaInicio, r.FechaFin)))
            throw new ConflictException("Las fechas se solapan con otra solicitud pendiente o aprobada.");

        var saldo = await CalcularSaldo(empleado, ct);
        var solicitud = SolicitudVacaciones.Crear(empleadoId, r.FechaInicio, r.FechaFin, r.Motivo, saldo.DiasDisponibles);
        db.SolicitudesVacaciones.Add(solicitud);
        await db.SaveChangesAsync(ct);
        return ToDto(solicitud, empleado);
    }

    public Task<SolicitudDto> AprobarAsync(int id, RevisionRequest r, CancellationToken ct) =>
        Revisar(id, s => s.Aprobar(currentUser.UserId, r.Comentario), ct);

    public Task<SolicitudDto> RechazarAsync(int id, RevisionRequest r, CancellationToken ct) =>
        Revisar(id, s => s.Rechazar(currentUser.UserId, r.Comentario), ct);

    /// <summary>Solo el propio empleado (o RR.HH.) puede cancelar, y únicamente mientras esté pendiente.</summary>
    public async Task<SolicitudDto> CancelarAsync(int id, CancellationToken ct)
    {
        var s = await Cargar(id, ct);
        if (!currentUser.EsGestor && s.EmpleadoId != currentUser.EmpleadoId) throw new ForbiddenException();
        s.Cancelar();
        await db.SaveChangesAsync(ct);
        return ToDto(s, s.Empleado!);
    }

    private async Task<SolicitudDto> Revisar(int id, Action<SolicitudVacaciones> accion, CancellationToken ct)
    {
        var s = await Cargar(id, ct);
        accion(s);
        await db.SaveChangesAsync(ct);
        return ToDto(s, s.Empleado!);
    }

    private async Task<SolicitudVacaciones> Cargar(int id, CancellationToken ct) =>
        await db.SolicitudesVacaciones.Include(s => s.Empleado).FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new NotFoundException("Solicitud de vacaciones", id);

    /// <summary>Un empleado solo opera sobre sí mismo; RR.HH./Admin pueden indicar cualquier empleado.</summary>
    private int ResolverEmpleado(int? solicitado)
    {
        if (currentUser.EsGestor)
            return solicitado ?? currentUser.EmpleadoId ?? throw new DomainException("Indique el empleado.");
        var propio = currentUser.EmpleadoId ?? throw new ForbiddenException("Su usuario no está vinculado a un empleado.");
        if (solicitado is not null && solicitado != propio) throw new ForbiddenException();
        return propio;
    }

    private async Task<SaldoVacacionesDto> CalcularSaldo(Empleado empleado, CancellationToken ct)
    {
        var ganados = empleado.DiasVacacionesGanados(clock.Hoy);
        var solicitudes = await db.SolicitudesVacaciones.AsNoTracking()
            .Where(s => s.EmpleadoId == empleado.Id && (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada))
            .ToListAsync(ct);
        var consumidos = solicitudes.Sum(s => s.Dias);
        return new SaldoVacacionesDto(empleado.Id, ganados, consumidos, Math.Max(0, ganados - consumidos));
    }

    private static SolicitudDto ToDto(SolicitudVacaciones s, Empleado e) => new(
        s.Id, s.EmpleadoId, e.NombreCompleto, s.FechaInicio, s.FechaFin, s.Dias, s.Estado.ToString(),
        s.Motivo, s.ComentarioRevision, s.FechaRevision, s.CreadoEn);
}

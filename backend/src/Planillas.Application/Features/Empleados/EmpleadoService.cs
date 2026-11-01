using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;

namespace Planillas.Application.Features.Empleados;

public class EmpleadoQuery : PageQuery
{
    public int? AreaId { get; set; }
    public RegimenPensionario? Regimen { get; set; }
    public bool? Activo { get; set; }
}

public record EmpleadoListDto(
    int Id, string Dni, string NombreCompleto, string Email, string Area, string Cargo,
    decimal SueldoBasico, string Regimen, string? Afp, DateOnly FechaIngreso, bool Activo);

public record EmpleadoDetailDto(
    int Id, string Dni, string Nombres, string Apellidos, string Email, DateOnly FechaIngreso,
    int AreaId, string Area, int CargoId, string Cargo, decimal SueldoBasico, bool TieneHijos,
    string Regimen, int? AfpId, string? Afp, string? Cuspp, bool Activo);

public record EmpleadoRequest(
    string Dni, string Nombres, string Apellidos, string Email, DateOnly FechaIngreso,
    int AreaId, int CargoId, decimal SueldoBasico, bool TieneHijos,
    RegimenPensionario Regimen, int? AfpId, string? Cuspp);

public class EmpleadoRequestValidator : AbstractValidator<EmpleadoRequest>
{
    public EmpleadoRequestValidator()
    {
        RuleFor(x => x.Dni).NotEmpty().Matches(@"^\d{8}$").WithMessage("El DNI debe tener 8 dígitos.");
        RuleFor(x => x.Nombres).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellidos).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.AreaId).GreaterThan(0);
        RuleFor(x => x.CargoId).GreaterThan(0);
        RuleFor(x => x.SueldoBasico).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.Regimen).IsInEnum();
        RuleFor(x => x.AfpId).NotNull().When(x => x.Regimen == RegimenPensionario.Afp)
            .WithMessage("Seleccione una AFP para el régimen privado.");
        RuleFor(x => x.Cuspp).MaximumLength(12);
    }
}

public class EmpleadoService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IValidator<EmpleadoRequest> validator)
{
    private static readonly Dictionary<string, Expression<Func<Empleado, object>>> Columnas = new()
    {
        ["nombre"] = e => e.Apellidos,
        ["dni"] = e => e.Dni,
        ["sueldo"] = e => e.SueldoBasico,
        ["ingreso"] = e => e.FechaIngreso,
        ["area"] = e => e.Area!.Nombre
    };

    public async Task<PagedResult<EmpleadoListDto>> ListAsync(EmpleadoQuery q, CancellationToken ct)
    {
        var query = db.Empleados.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(e => e.Nombres.ToLower().Contains(s) || e.Apellidos.ToLower().Contains(s)
                                     || e.Dni.Contains(s) || e.Email.ToLower().Contains(s));
        }
        if (q.AreaId is not null) query = query.Where(e => e.AreaId == q.AreaId);
        if (q.Regimen is not null) query = query.Where(e => e.Regimen == q.Regimen);
        if (q.Activo is not null) query = query.Where(e => e.Activo == q.Activo);

        return await query.OrdenarPor(q, Columnas, "nombre")
            .Select(e => new EmpleadoListDto(e.Id, e.Dni, e.Nombres + " " + e.Apellidos, e.Email,
                e.Area!.Nombre, e.Cargo!.Nombre, e.SueldoBasico, e.Regimen.ToString(),
                e.Afp != null ? e.Afp.Nombre : null, e.FechaIngreso, e.Activo))
            .ToPagedResultAsync(q, ct);
    }

    public async Task<EmpleadoDetailDto> GetAsync(int id, CancellationToken ct)
    {
        var e = await Cargar(id, ct);
        return ToDetail(e);
    }

    /// <summary>Ficha propia del empleado autenticado (rol Empleado).</summary>
    public async Task<EmpleadoDetailDto> GetMineAsync(CancellationToken ct)
    {
        var id = currentUser.EmpleadoId ?? throw new ForbiddenException("Su usuario no está vinculado a un empleado.");
        return await GetAsync(id, ct);
    }

    public async Task<EmpleadoDetailDto> CreateAsync(EmpleadoRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        await ValidarReferencias(r, ct);
        if (await db.Empleados.AnyAsync(e => e.Dni == r.Dni, ct))
            throw new ConflictException($"Ya existe un empleado con DNI {r.Dni}.");

        var empleado = new Empleado();
        Aplicar(empleado, r);
        db.Empleados.Add(empleado);
        await db.SaveChangesAsync(ct);
        return await GetAsync(empleado.Id, ct);
    }

    public async Task<EmpleadoDetailDto> UpdateAsync(int id, EmpleadoRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        var empleado = await db.Empleados.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Empleado", id);
        await ValidarReferencias(r, ct);
        if (await db.Empleados.AnyAsync(e => e.Dni == r.Dni && e.Id != id, ct))
            throw new ConflictException($"Ya existe otro empleado con DNI {r.Dni}.");

        Aplicar(empleado, r);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// Baja lógica: un empleado con boletas emitidas no puede borrarse físicamente
    /// (las boletas son documentos laborales que deben conservarse).
    /// </summary>
    public async Task DesactivarAsync(int id, CancellationToken ct)
    {
        var empleado = await db.Empleados.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Empleado", id);
        empleado.Activo = false;
        await db.SaveChangesAsync(ct);
    }

    private static void Aplicar(Empleado e, EmpleadoRequest r)
    {
        e.Dni = r.Dni.Trim();
        e.Nombres = r.Nombres.Trim();
        e.Apellidos = r.Apellidos.Trim();
        e.Email = r.Email.Trim().ToLowerInvariant();
        e.FechaIngreso = r.FechaIngreso;
        e.AreaId = r.AreaId;
        e.CargoId = r.CargoId;
        e.SueldoBasico = r.SueldoBasico;
        e.TieneHijos = r.TieneHijos;
        e.Regimen = r.Regimen;
        e.AfpId = r.AfpId;
        e.Cuspp = r.Cuspp;
        e.NormalizarRegimen();
    }

    private async Task ValidarReferencias(EmpleadoRequest r, CancellationToken ct)
    {
        if (!await db.Areas.AnyAsync(a => a.Id == r.AreaId, ct)) throw new NotFoundException("Área", r.AreaId);
        if (!await db.Cargos.AnyAsync(c => c.Id == r.CargoId, ct)) throw new NotFoundException("Cargo", r.CargoId);
        if (r.AfpId is not null && !await db.Afps.AnyAsync(a => a.Id == r.AfpId, ct))
            throw new NotFoundException("AFP", r.AfpId);
    }

    private async Task<Empleado> Cargar(int id, CancellationToken ct) =>
        await db.Empleados.AsNoTracking().Include(e => e.Area).Include(e => e.Cargo).Include(e => e.Afp)
            .FirstOrDefaultAsync(e => e.Id == id, ct) ?? throw new NotFoundException("Empleado", id);

    private static EmpleadoDetailDto ToDetail(Empleado e) => new(
        e.Id, e.Dni, e.Nombres, e.Apellidos, e.Email, e.FechaIngreso, e.AreaId, e.Area?.Nombre ?? "",
        e.CargoId, e.Cargo?.Nombre ?? "", e.SueldoBasico, e.TieneHijos, e.Regimen.ToString(),
        e.AfpId, e.Afp?.Nombre, e.Cuspp, e.Activo);
}

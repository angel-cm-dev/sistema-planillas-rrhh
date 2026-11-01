using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;
using Planillas.Domain.Entities;

namespace Planillas.Application.Features.Catalogos;

public record CatalogoDto(int Id, string Nombre);
public record CatalogoRequest(string Nombre);
public record AfpDto(int Id, string Nombre, decimal AportePct, decimal PrimaSeguroPct, decimal ComisionPct);
public record AfpRequest(decimal AportePct, decimal PrimaSeguroPct, decimal ComisionPct);
public record ParametrosDto(
    decimal Uit, decimal Rmv, decimal AsignacionFamiliarPct, decimal OnpPct, decimal EsSaludPct,
    decimal HoraExtra25Pct, decimal HoraExtra35Pct, decimal TopePrimaAfp, decimal HorasJornadaDiaria);

public class CatalogoRequestValidator : AbstractValidator<CatalogoRequest>
{
    public CatalogoRequestValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public class AfpRequestValidator : AbstractValidator<AfpRequest>
{
    public AfpRequestValidator()
    {
        RuleFor(x => x.AportePct).InclusiveBetween(0, 30);
        RuleFor(x => x.PrimaSeguroPct).InclusiveBetween(0, 10);
        RuleFor(x => x.ComisionPct).InclusiveBetween(0, 10);
    }
}

public class ParametrosValidator : AbstractValidator<ParametrosDto>
{
    public ParametrosValidator()
    {
        RuleFor(x => x.Uit).GreaterThan(0);
        RuleFor(x => x.Rmv).GreaterThan(0);
        RuleFor(x => x.AsignacionFamiliarPct).InclusiveBetween(0, 100);
        RuleFor(x => x.OnpPct).InclusiveBetween(0, 100);
        RuleFor(x => x.EsSaludPct).InclusiveBetween(0, 100);
        RuleFor(x => x.HoraExtra25Pct).InclusiveBetween(0, 200);
        RuleFor(x => x.HoraExtra35Pct).InclusiveBetween(0, 200);
        RuleFor(x => x.TopePrimaAfp).GreaterThan(0);
        RuleFor(x => x.HorasJornadaDiaria).InclusiveBetween(1, 12);
    }
}

public class CatalogoService(
    IAppDbContext db,
    IValidator<CatalogoRequest> catalogoValidator,
    IValidator<AfpRequest> afpValidator,
    IValidator<ParametrosDto> parametrosValidator)
{
    public async Task<IReadOnlyList<CatalogoDto>> AreasAsync(CancellationToken ct) =>
        await db.Areas.AsNoTracking().OrderBy(a => a.Nombre).Select(a => new CatalogoDto(a.Id, a.Nombre)).ToListAsync(ct);

    public async Task<IReadOnlyList<CatalogoDto>> CargosAsync(CancellationToken ct) =>
        await db.Cargos.AsNoTracking().OrderBy(a => a.Nombre).Select(a => new CatalogoDto(a.Id, a.Nombre)).ToListAsync(ct);

    public async Task<CatalogoDto> CrearAreaAsync(CatalogoRequest r, CancellationToken ct)
    {
        await catalogoValidator.ValidateAndThrowAsync(r, ct);
        if (await db.Areas.AnyAsync(a => a.Nombre == r.Nombre.Trim(), ct))
            throw new ConflictException($"El área '{r.Nombre}' ya existe.");
        var area = new Area { Nombre = r.Nombre.Trim() };
        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);
        return new CatalogoDto(area.Id, area.Nombre);
    }

    public async Task<CatalogoDto> CrearCargoAsync(CatalogoRequest r, CancellationToken ct)
    {
        await catalogoValidator.ValidateAndThrowAsync(r, ct);
        if (await db.Cargos.AnyAsync(a => a.Nombre == r.Nombre.Trim(), ct))
            throw new ConflictException($"El cargo '{r.Nombre}' ya existe.");
        var cargo = new Cargo { Nombre = r.Nombre.Trim() };
        db.Cargos.Add(cargo);
        await db.SaveChangesAsync(ct);
        return new CatalogoDto(cargo.Id, cargo.Nombre);
    }

    public async Task<IReadOnlyList<AfpDto>> AfpsAsync(CancellationToken ct) =>
        await db.Afps.AsNoTracking().OrderBy(a => a.Nombre)
            .Select(a => new AfpDto(a.Id, a.Nombre, a.AportePct, a.PrimaSeguroPct, a.ComisionPct)).ToListAsync(ct);

    public async Task<AfpDto> ActualizarAfpAsync(int id, AfpRequest r, CancellationToken ct)
    {
        await afpValidator.ValidateAndThrowAsync(r, ct);
        var afp = await db.Afps.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException("AFP", id);
        afp.AportePct = r.AportePct;
        afp.PrimaSeguroPct = r.PrimaSeguroPct;
        afp.ComisionPct = r.ComisionPct;
        await db.SaveChangesAsync(ct);
        return new AfpDto(afp.Id, afp.Nombre, afp.AportePct, afp.PrimaSeguroPct, afp.ComisionPct);
    }

    public async Task<ParametrosDto> ParametrosAsync(CancellationToken ct) => ToDto(await ObtenerParametros(db, ct));

    public async Task<ParametrosDto> ActualizarParametrosAsync(ParametrosDto r, CancellationToken ct)
    {
        await parametrosValidator.ValidateAndThrowAsync(r, ct);
        var p = await ObtenerParametros(db, ct);
        p.Uit = r.Uit;
        p.Rmv = r.Rmv;
        p.AsignacionFamiliarPct = r.AsignacionFamiliarPct;
        p.OnpPct = r.OnpPct;
        p.EsSaludPct = r.EsSaludPct;
        p.HoraExtra25Pct = r.HoraExtra25Pct;
        p.HoraExtra35Pct = r.HoraExtra35Pct;
        p.TopePrimaAfp = r.TopePrimaAfp;
        p.HorasJornadaDiaria = r.HorasJornadaDiaria;
        await db.SaveChangesAsync(ct);
        return ToDto(p);
    }

    /// <summary>Los parámetros son una única fila creada por el seed.</summary>
    internal static async Task<ParametrosPlanilla> ObtenerParametros(IAppDbContext db, CancellationToken ct) =>
        await db.Parametros.OrderBy(p => p.Id).FirstOrDefaultAsync(ct)
        ?? throw new InvalidOperationException("No hay parámetros de planilla configurados (¿se ejecutó el seed?).");

    private static ParametrosDto ToDto(ParametrosPlanilla p) => new(
        p.Uit, p.Rmv, p.AsignacionFamiliarPct, p.OnpPct, p.EsSaludPct,
        p.HoraExtra25Pct, p.HoraExtra35Pct, p.TopePrimaAfp, p.HorasJornadaDiaria);
}

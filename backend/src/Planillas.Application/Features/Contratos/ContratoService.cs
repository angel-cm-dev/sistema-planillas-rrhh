using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;

namespace Planillas.Application.Features.Contratos;

public class ContratoQuery : PageQuery
{
    public int? EmpleadoId { get; set; }
    public EstadoContrato? Estado { get; set; }
    public TipoContrato? Tipo { get; set; }
}

public record ContratoDto(
    int Id, int EmpleadoId, string Empleado, string Tipo, DateOnly FechaInicio, DateOnly? FechaFin,
    decimal Sueldo, string Estado);

public record ContratoRequest(int EmpleadoId, TipoContrato Tipo, DateOnly FechaInicio, DateOnly? FechaFin, decimal Sueldo);
public record FinalizarContratoRequest(DateOnly FechaCese);

public class ContratoRequestValidator : AbstractValidator<ContratoRequest>
{
    public ContratoRequestValidator()
    {
        RuleFor(x => x.EmpleadoId).GreaterThan(0);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Sueldo).GreaterThan(0);
        RuleFor(x => x.FechaFin).NotNull().When(x => x.Tipo == TipoContrato.PlazoFijo)
            .WithMessage("Un contrato a plazo fijo requiere fecha de fin.");
    }
}

public class ContratoService(IAppDbContext db, IValidator<ContratoRequest> validator)
{
    private static readonly Dictionary<string, Expression<Func<Contrato, object>>> Columnas = new()
    {
        ["inicio"] = c => c.FechaInicio,
        ["empleado"] = c => c.Empleado!.Apellidos,
        ["sueldo"] = c => c.Sueldo
    };

    public async Task<PagedResult<ContratoDto>> ListAsync(ContratoQuery q, CancellationToken ct)
    {
        var query = db.Contratos.AsNoTracking().AsQueryable();
        if (q.EmpleadoId is not null) query = query.Where(c => c.EmpleadoId == q.EmpleadoId);
        if (q.Estado is not null) query = query.Where(c => c.Estado == q.Estado);
        if (q.Tipo is not null) query = query.Where(c => c.Tipo == q.Tipo);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(c => c.Empleado!.Nombres.ToLower().Contains(s) || c.Empleado.Apellidos.ToLower().Contains(s));
        }
        if (q.SortBy is null) q.SortDir ??= "desc";

        return await query.OrdenarPor(q, Columnas, "inicio")
            .Select(c => new ContratoDto(c.Id, c.EmpleadoId, c.Empleado!.Nombres + " " + c.Empleado.Apellidos,
                c.Tipo.ToString(), c.FechaInicio, c.FechaFin, c.Sueldo, c.Estado.ToString()))
            .ToPagedResultAsync(q, ct);
    }

    /// <summary>
    /// Registrar un contrato nuevo actualiza el sueldo básico del empleado: el contrato vigente
    /// es la fuente de verdad de la remuneración pactada. Solo puede haber UN contrato vigente.
    /// </summary>
    public async Task<ContratoDto> CreateAsync(ContratoRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        var empleado = await db.Empleados.FirstOrDefaultAsync(e => e.Id == r.EmpleadoId, ct)
            ?? throw new NotFoundException("Empleado", r.EmpleadoId);
        if (await db.Contratos.AnyAsync(c => c.EmpleadoId == r.EmpleadoId && c.Estado == EstadoContrato.Vigente, ct))
            throw new ConflictException("El empleado ya tiene un contrato vigente. Finalícelo antes de registrar otro.");

        var contrato = new Contrato
        {
            EmpleadoId = r.EmpleadoId, Tipo = r.Tipo, FechaInicio = r.FechaInicio, FechaFin = r.FechaFin, Sueldo = r.Sueldo
        };
        contrato.Validar();
        empleado.SueldoBasico = r.Sueldo;
        empleado.Activo = true;
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync(ct);
        return new ContratoDto(contrato.Id, empleado.Id, empleado.NombreCompleto, contrato.Tipo.ToString(),
            contrato.FechaInicio, contrato.FechaFin, contrato.Sueldo, contrato.Estado.ToString());
    }

    public async Task<ContratoDto> FinalizarAsync(int id, FinalizarContratoRequest r, CancellationToken ct)
    {
        var contrato = await db.Contratos.Include(c => c.Empleado).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Contrato", id);
        contrato.Finalizar(r.FechaCese);
        await db.SaveChangesAsync(ct);
        return new ContratoDto(contrato.Id, contrato.EmpleadoId, contrato.Empleado!.NombreCompleto, contrato.Tipo.ToString(),
            contrato.FechaInicio, contrato.FechaFin, contrato.Sueldo, contrato.Estado.ToString());
    }
}

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planillas.Application.Common;
using Planillas.Application.Features.Catalogos;
using Planillas.Domain.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;
using Planillas.Domain.Payroll;

namespace Planillas.Application.Features.Planillas;

public class PlanillaQuery : PageQuery
{
    public int? Anio { get; set; }
    public EstadoPlanilla? Estado { get; set; }
}

public record GenerarPlanillaRequest(int Anio, int Mes);

public record PlanillaResumenDto(
    int Id, int Anio, int Mes, string Periodo, string Estado, int NumeroBoletas,
    decimal TotalIngresos, decimal TotalDescuentos, decimal TotalNeto, decimal TotalEsSalud, DateTime CreadoEn, DateTime? FechaCierre);

public record BoletaResumenDto(
    int Id, int PlanillaId, string Periodo, string EstadoPlanilla, int EmpleadoId, string Empleado, string Dni, string Regimen,
    decimal TotalIngresos, decimal TotalDescuentos, decimal NetoPagar, decimal EsSalud);

public record PlanillaDetalleDto(PlanillaResumenDto Planilla, IReadOnlyList<BoletaResumenDto> Boletas);

public record BoletaDto(
    int Id, int PlanillaId, string Periodo, string EstadoPlanilla,
    int EmpleadoId, string EmpleadoNombre, string EmpleadoDni, string Cargo, string Area, string Regimen, string? AfpNombre,
    decimal SueldoBasico, decimal AsignacionFamiliar, decimal HorasExtra25, decimal HorasExtra35, decimal MontoHorasExtra,
    int DiasFalta, decimal DescuentoFaltas, decimal TotalIngresos,
    decimal Onp, decimal AfpAporte, decimal AfpPrima, decimal AfpComision, decimal Renta5ta, decimal TotalDescuentos,
    decimal NetoPagar, decimal EsSalud);

public record SimulacionRequest(
    decimal SueldoBasico, bool TieneHijos, RegimenPensionario Regimen, int? AfpId, int DiasFalta,
    IReadOnlyList<decimal>? HorasExtraPorDia);

public class GenerarPlanillaValidator : AbstractValidator<GenerarPlanillaRequest>
{
    public GenerarPlanillaValidator()
    {
        RuleFor(x => x.Anio).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Mes).InclusiveBetween(1, 12);
    }
}

public class SimulacionValidator : AbstractValidator<SimulacionRequest>
{
    public SimulacionValidator()
    {
        RuleFor(x => x.SueldoBasico).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.DiasFalta).InclusiveBetween(0, 30);
        RuleFor(x => x.AfpId).NotNull().When(x => x.Regimen == RegimenPensionario.Afp)
            .WithMessage("Seleccione una AFP para el régimen privado.");
        RuleForEach(x => x.HorasExtraPorDia).InclusiveBetween(0, 12);
    }
}

public class PlanillaService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IValidator<GenerarPlanillaRequest> generarValidator,
    IValidator<SimulacionRequest> simulacionValidator,
    ILogger<PlanillaService> logger)
{
    public async Task<PagedResult<PlanillaResumenDto>> ListAsync(PlanillaQuery q, CancellationToken ct)
    {
        var query = db.Planillas.AsNoTracking().AsQueryable();
        if (q.Anio is not null) query = query.Where(p => p.Anio == q.Anio);
        if (q.Estado is not null) query = query.Where(p => p.Estado == q.Estado);
        var pagina = await query.OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes)
            .Select(p => new PlanillaResumenDto(p.Id, p.Anio, p.Mes, "", p.Estado.ToString(), p.Boletas.Count,
                p.Boletas.Sum(b => b.TotalIngresos), p.Boletas.Sum(b => b.TotalDescuentos),
                p.Boletas.Sum(b => b.NetoPagar), p.Boletas.Sum(b => b.EsSalud), p.CreadoEn, p.FechaCierre))
            .ToPagedResultAsync(q, ct);
        // El texto del periodo se arma en memoria: formatear dentro de la consulta no es traducible a SQL.
        return pagina with { Items = pagina.Items.Select(i => i with { Periodo = $"{i.Anio}-{i.Mes:00}" }).ToList() };
    }

    public async Task<PlanillaDetalleDto> GetAsync(int id, CancellationToken ct)
    {
        var p = await db.Planillas.AsNoTracking().Include(x => x.Boletas)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Planilla", id);
        return new PlanillaDetalleDto(ToResumen(p),
            p.Boletas.OrderBy(b => b.EmpleadoNombre).Select(b => ToBoletaResumen(b, p)).ToList());
    }

    /// <summary>
    /// Genera (o regenera mientras está en Borrador) la planilla de un periodo:
    /// toma empleados activos, consolida la asistencia del mes y aplica la calculadora a cada uno.
    /// </summary>
    public async Task<PlanillaDetalleDto> GenerarAsync(GenerarPlanillaRequest r, CancellationToken ct)
    {
        await generarValidator.ValidateAndThrowAsync(r, ct);
        var inicioMes = new DateOnly(r.Anio, r.Mes, 1);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);
        if (inicioMes > clock.Hoy)
            throw new DomainException("No se puede generar la planilla de un periodo futuro.");

        var planilla = await db.Planillas.Include(p => p.Boletas)
            .FirstOrDefaultAsync(p => p.Anio == r.Anio && p.Mes == r.Mes, ct);
        if (planilla is null)
        {
            planilla = new Planilla { Anio = r.Anio, Mes = r.Mes };
            db.Planillas.Add(planilla);
        }
        else
        {
            planilla.AsegurarEditable();
            // Regenerar = descartar las boletas del borrador y volver a calcular con los datos actuales.
            db.Boletas.RemoveRange(planilla.Boletas);
            planilla.Boletas.Clear();
        }

        var parametros = await CatalogoService.ObtenerParametros(db, ct);
        var empleados = await db.Empleados.AsNoTracking()
            .Include(e => e.Area).Include(e => e.Cargo).Include(e => e.Afp)
            .Where(e => e.Activo && e.FechaIngreso <= finMes)
            .ToListAsync(ct);
        var asistencias = await db.Asistencias.AsNoTracking()
            .Where(a => a.Fecha >= inicioMes && a.Fecha <= finMes)
            .ToListAsync(ct);
        var porEmpleado = asistencias.ToLookup(a => a.EmpleadoId);

        foreach (var e in empleados)
        {
            var registros = porEmpleado[e.Id].ToList();
            var entrada = new EntradaCalculo(
                e.SueldoBasico,
                e.TieneHijos,
                e.Regimen,
                e.Afp is null ? null : new TasasAfp(e.Afp.Nombre, e.Afp.AportePct, e.Afp.PrimaSeguroPct, e.Afp.ComisionPct),
                registros.Count(a => a.Estado == EstadoAsistencia.Falta),
                registros.Where(a => a.HorasExtra > 0).Select(a => a.HorasExtra).ToList());
            var c = CalculadoraPlanilla.Calcular(entrada, parametros);

            planilla.Boletas.Add(new BoletaPago
            {
                EmpleadoId = e.Id,
                EmpleadoNombre = e.NombreCompleto,
                EmpleadoDni = e.Dni,
                Cargo = e.Cargo?.Nombre ?? "",
                Area = e.Area?.Nombre ?? "",
                Regimen = e.Regimen,
                AfpNombre = e.Afp?.Nombre,
                SueldoBasico = c.SueldoBasico,
                AsignacionFamiliar = c.AsignacionFamiliar,
                HorasExtra25 = c.HorasExtra25,
                HorasExtra35 = c.HorasExtra35,
                MontoHorasExtra = c.MontoHorasExtra,
                DiasFalta = c.DiasFalta,
                DescuentoFaltas = c.DescuentoFaltas,
                TotalIngresos = c.TotalIngresos,
                Onp = c.Onp,
                AfpAporte = c.AfpAporte,
                AfpPrima = c.AfpPrima,
                AfpComision = c.AfpComision,
                Renta5ta = c.Renta5ta,
                TotalDescuentos = c.TotalDescuentos,
                NetoPagar = c.NetoPagar,
                EsSalud = c.EsSalud
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Planilla {Periodo} generada con {Boletas} boletas por el usuario {UserId}",
            planilla.Periodo, planilla.Boletas.Count, currentUser.UserId);
        return await GetAsync(planilla.Id, ct);
    }

    public async Task<PlanillaResumenDto> CerrarAsync(int id, CancellationToken ct)
    {
        var p = await db.Planillas.Include(x => x.Boletas).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Planilla", id);
        p.Cerrar();
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Planilla {Periodo} cerrada por el usuario {UserId}", p.Periodo, currentUser.UserId);
        return ToResumen(p);
    }

    /// <summary>Calculadora didáctica: aplica las mismas reglas de la planilla sin guardar nada.</summary>
    public async Task<ResultadoCalculo> SimularAsync(SimulacionRequest r, CancellationToken ct)
    {
        await simulacionValidator.ValidateAndThrowAsync(r, ct);
        TasasAfp? tasas = null;
        if (r.Regimen == RegimenPensionario.Afp)
        {
            var afp = await db.Afps.AsNoTracking().FirstOrDefaultAsync(a => a.Id == r.AfpId, ct)
                ?? throw new NotFoundException("AFP", r.AfpId!);
            tasas = new TasasAfp(afp.Nombre, afp.AportePct, afp.PrimaSeguroPct, afp.ComisionPct);
        }
        var parametros = await CatalogoService.ObtenerParametros(db, ct);
        return CalculadoraPlanilla.Calcular(
            new EntradaCalculo(r.SueldoBasico, r.TieneHijos, r.Regimen, tasas, r.DiasFalta, r.HorasExtraPorDia ?? []),
            parametros);
    }

    /// <summary>
    /// Boletas del empleado autenticado. Solo se muestran las de planillas CERRADAS:
    /// un borrador todavía puede cambiar y no debe tomarse como documento de pago.
    /// </summary>
    public async Task<IReadOnlyList<BoletaResumenDto>> MisBoletasAsync(CancellationToken ct)
    {
        var empleadoId = currentUser.EmpleadoId ?? throw new ForbiddenException("Su usuario no está vinculado a un empleado.");
        var boletas = await db.Boletas.AsNoTracking().Include(b => b.Planilla)
            .Where(b => b.EmpleadoId == empleadoId && b.Planilla!.Estado == EstadoPlanilla.Cerrada)
            .ToListAsync(ct);
        return boletas.OrderByDescending(b => b.Planilla!.Anio).ThenByDescending(b => b.Planilla!.Mes)
            .Select(b => ToBoletaResumen(b, b.Planilla!)).ToList();
    }

    public async Task<BoletaDto> GetBoletaAsync(int id, CancellationToken ct)
    {
        var b = await db.Boletas.AsNoTracking().Include(x => x.Planilla).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Boleta", id);

        // Regla de privacidad: un empleado solo accede a SUS boletas y solo de planillas cerradas.
        if (!currentUser.EsGestor &&
            (b.EmpleadoId != currentUser.EmpleadoId || b.Planilla!.Estado != EstadoPlanilla.Cerrada))
            throw new ForbiddenException("Solo puede consultar sus propias boletas.");

        return new BoletaDto(b.Id, b.PlanillaId, b.Planilla!.Periodo, b.Planilla.Estado.ToString(),
            b.EmpleadoId, b.EmpleadoNombre, b.EmpleadoDni, b.Cargo, b.Area, b.Regimen.ToString(), b.AfpNombre,
            b.SueldoBasico, b.AsignacionFamiliar, b.HorasExtra25, b.HorasExtra35, b.MontoHorasExtra,
            b.DiasFalta, b.DescuentoFaltas, b.TotalIngresos,
            b.Onp, b.AfpAporte, b.AfpPrima, b.AfpComision, b.Renta5ta, b.TotalDescuentos, b.NetoPagar, b.EsSalud);
    }

    private static PlanillaResumenDto ToResumen(Planilla p) => new(
        p.Id, p.Anio, p.Mes, p.Periodo, p.Estado.ToString(), p.Boletas.Count,
        p.Boletas.Sum(b => b.TotalIngresos), p.Boletas.Sum(b => b.TotalDescuentos),
        p.Boletas.Sum(b => b.NetoPagar), p.Boletas.Sum(b => b.EsSalud), p.CreadoEn, p.FechaCierre);

    private static BoletaResumenDto ToBoletaResumen(BoletaPago b, Planilla p) => new(
        b.Id, p.Id, p.Periodo, p.Estado.ToString(), b.EmpleadoId, b.EmpleadoNombre, b.EmpleadoDni, b.Regimen.ToString(),
        b.TotalIngresos, b.TotalDescuentos, b.NetoPagar, b.EsSalud);
}

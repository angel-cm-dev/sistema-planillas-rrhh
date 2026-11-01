using Microsoft.EntityFrameworkCore;
using Planillas.Application.Common;
using Planillas.Domain.Enums;

namespace Planillas.Application.Features.Dashboard;

public record ConteoDto(string Etiqueta, int Cantidad);
public record PeriodoMontoDto(string Periodo, decimal Neto, decimal CostoEmpleador);

public record DashboardDto(
    int EmpleadosActivos,
    int EmpleadosInactivos,
    decimal MasaSalarialMensual,
    int SolicitudesPendientes,
    int ContratosPorVencer,
    string? UltimoPeriodo,
    decimal UltimoNeto,
    decimal UltimoCostoEmpleador,
    IReadOnlyList<ConteoDto> EmpleadosPorArea,
    IReadOnlyList<ConteoDto> DistribucionRegimen,
    IReadOnlyList<PeriodoMontoDto> Historico);

public class DashboardService(IAppDbContext db, IClock clock)
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct)
    {
        // Volúmenes pequeños (cientos de empleados): materializar y agregar en memoria es simple y
        // portable entre SQL Server y SQLite. Con miles de filas convendría agregar en SQL.
        var empleados = await db.Empleados.AsNoTracking()
            .Select(e => new { e.Activo, e.SueldoBasico, e.Regimen, Area = e.Area!.Nombre })
            .ToListAsync(ct);
        var activos = empleados.Where(e => e.Activo).ToList();

        var pendientes = await db.SolicitudesVacaciones.CountAsync(s => s.Estado == EstadoSolicitud.Pendiente, ct);
        var limite = clock.Hoy.AddDays(30);
        var porVencer = await db.Contratos.CountAsync(c =>
            c.Estado == EstadoContrato.Vigente && c.FechaFin != null && c.FechaFin <= limite, ct);

        var planillas = await db.Planillas.AsNoTracking()
            .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes).Take(6)
            .Select(p => new
            {
                p.Anio, p.Mes,
                Neto = p.Boletas.Sum(b => b.NetoPagar),
                Ingresos = p.Boletas.Sum(b => b.TotalIngresos),
                EsSalud = p.Boletas.Sum(b => b.EsSalud)
            })
            .ToListAsync(ct);
        var historico = planillas
            .OrderBy(p => p.Anio).ThenBy(p => p.Mes)
            .Select(p => new PeriodoMontoDto($"{p.Anio}-{p.Mes:00}", p.Neto, p.Ingresos + p.EsSalud))
            .ToList();
        var ultima = historico.LastOrDefault();

        return new DashboardDto(
            activos.Count,
            empleados.Count - activos.Count,
            activos.Sum(e => e.SueldoBasico),
            pendientes,
            porVencer,
            ultima?.Periodo,
            ultima?.Neto ?? 0,
            ultima?.CostoEmpleador ?? 0,
            activos.GroupBy(e => e.Area).Select(g => new ConteoDto(g.Key, g.Count())).OrderByDescending(c => c.Cantidad).ToList(),
            activos.GroupBy(e => e.Regimen).Select(g => new ConteoDto(g.Key.ToString(), g.Count())).ToList(),
            historico);
    }
}

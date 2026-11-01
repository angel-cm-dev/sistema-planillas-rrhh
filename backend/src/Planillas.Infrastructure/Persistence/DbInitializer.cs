using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planillas.Application.Common;
using Planillas.Domain.Entities;
using Planillas.Domain.Enums;
using Planillas.Domain.Payroll;

namespace Planillas.Infrastructure.Persistence;

/// <summary>
/// Crea el esquema y carga datos de ejemplo FICTICIOS. Todos los nombres, DNI y correos son inventados.
/// - SQL Server: aplica las migraciones de EF Core (esquema versionado).
/// - SQLite (pruebas / desarrollo local): EnsureCreated, porque las migraciones son específicas de SQL Server.
/// </summary>
public class DbInitializer(AppDbContext db, IPasswordHasher hasher, IClock clock, ILogger<DbInitializer> logger)
{
    public const string PasswordDemo = "Demo123!";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (db.Database.IsSqlServer())
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);

        if (await db.Usuarios.AnyAsync(ct))
        {
            logger.LogInformation("La base de datos ya contiene datos; se omite el seed.");
            return;
        }
        await SeedAsync(ct);
        logger.LogInformation("Seed de datos de ejemplo completado.");
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        // Valores REFERENCIALES y configurables desde la pantalla de Parámetros.
        var parametros = new ParametrosPlanilla
        {
            Uit = 5350m, Rmv = 1130m, AsignacionFamiliarPct = 10m, OnpPct = 13m, EsSaludPct = 9m,
            HoraExtra25Pct = 25m, HoraExtra35Pct = 35m, TopePrimaAfp = 12234.34m, HorasJornadaDiaria = 8m
        };
        db.Parametros.Add(parametros);

        var afps = new[]
        {
            new Afp { Nombre = "Habitat", AportePct = 10m, PrimaSeguroPct = 1.37m, ComisionPct = 1.47m },
            new Afp { Nombre = "Integra", AportePct = 10m, PrimaSeguroPct = 1.37m, ComisionPct = 1.55m },
            new Afp { Nombre = "Prima", AportePct = 10m, PrimaSeguroPct = 1.37m, ComisionPct = 1.60m },
            new Afp { Nombre = "Profuturo", AportePct = 10m, PrimaSeguroPct = 1.37m, ComisionPct = 1.69m }
        };
        db.Afps.AddRange(afps);

        var areas = new[] { "Administración y Finanzas", "Recursos Humanos", "Tecnología", "Operaciones", "Comercial" }
            .Select(n => new Area { Nombre = n }).ToArray();
        db.Areas.AddRange(areas);

        var cargos = new[]
        {
            "Gerente de Área", "Analista de RR.HH.", "Desarrollador de Software", "Asistente Administrativo",
            "Ejecutivo Comercial", "Supervisor de Operaciones", "Contador", "Operario de Almacén"
        }.Select(n => new Cargo { Nombre = n }).ToArray();
        db.Cargos.AddRange(cargos);

        var hoy = clock.Hoy;
        // (dni, nombres, apellidos, área, cargo, sueldo, hijos, afp(null = ONP), años de antigüedad)
        var datos = new (string Dni, string Nombres, string Apellidos, int Area, int Cargo, decimal Sueldo, bool Hijos, int? Afp, int Anios)[]
        {
            ("70000001", "Lucía", "Quispe Huamán", 1, 1, 3200m, true, 1, 4),
            ("70000002", "Carlos", "Mendoza Ríos", 2, 2, 12500m, false, 2, 8),
            ("70000003", "Rosa", "Flores Paredes", 0, 6, 4800m, true, null, 6),
            ("70000004", "Jorge", "Ramírez Soto", 2, 2, 6500m, false, 0, 3),
            ("70000005", "Ana", "Torres Vargas", 4, 4, 2800m, true, 3, 2),
            ("70000006", "Miguel", "Castillo Rojas", 3, 5, 3600m, true, null, 5),
            ("70000007", "Patricia", "Chávez León", 0, 3, 1800m, false, 1, 1),
            ("70000008", "Luis", "Gutiérrez Salas", 3, 7, 1250m, true, null, 2),
            ("70000009", "Carmen", "Herrera Díaz", 4, 0, 9800m, true, 2, 7),
            ("70000010", "Diego", "Morales Cruz", 2, 2, 4200m, false, 0, 1),
            ("70000011", "Sofía", "Vega Campos", 1, 3, 1600m, false, null, 3),
            ("70000012", "Andrés", "Navarro Ponce", 3, 7, 1130m, true, 3, 2)
        };

        var empleados = datos.Select((d, i) => new Empleado
        {
            Dni = d.Dni, Nombres = d.Nombres, Apellidos = d.Apellidos,
            Email = $"{d.Nombres.ToLowerInvariant()}.{d.Apellidos.Split(' ')[0].ToLowerInvariant()}@empresa-demo.pe"
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u"),
            FechaIngreso = hoy.AddYears(-d.Anios).AddMonths(-(i % 6)),
            Area = areas[d.Area], Cargo = cargos[d.Cargo], SueldoBasico = d.Sueldo, TieneHijos = d.Hijos,
            Regimen = d.Afp is null ? RegimenPensionario.Onp : RegimenPensionario.Afp,
            Afp = d.Afp is null ? null : afps[d.Afp.Value],
            Cuspp = d.Afp is null ? null : $"{600000 + i}LQHX{i:00}"
        }).ToList();
        db.Empleados.AddRange(empleados);

        // Un contrato vigente por empleado; dos de plazo fijo, uno de ellos próximo a vencer.
        for (var i = 0; i < empleados.Count; i++)
        {
            var e = empleados[i];
            var plazoFijo = i is 6 or 9;
            db.Contratos.Add(new Contrato
            {
                Empleado = e,
                Tipo = plazoFijo ? TipoContrato.PlazoFijo : TipoContrato.Indeterminado,
                FechaInicio = e.FechaIngreso,
                FechaFin = plazoFijo ? hoy.AddDays(i == 6 ? 20 : 120) : null,
                Sueldo = e.SueldoBasico
            });
        }

        db.Usuarios.AddRange(
            new Usuario { Email = "admin@demo.pe", NombreCompleto = "Administrador Demo", Rol = Rol.Admin, PasswordHash = hasher.Hash(PasswordDemo) },
            new Usuario { Email = "rrhh@demo.pe", NombreCompleto = "Carlos Mendoza Ríos", Rol = Rol.Rrhh, PasswordHash = hasher.Hash(PasswordDemo), Empleado = empleados[1] },
            new Usuario { Email = "empleado@demo.pe", NombreCompleto = "Lucía Quispe Huamán", Rol = Rol.Empleado, PasswordHash = hasher.Hash(PasswordDemo), Empleado = empleados[0] });

        // Asistencia del mes anterior (días hábiles): algunas faltas y horas extra para que la planilla sea interesante.
        var mesAnterior = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-1);
        var rnd = new Random(2026); // semilla fija: datos reproducibles en cada instalación
        for (var dia = mesAnterior; dia < mesAnterior.AddMonths(1); dia = dia.AddDays(1))
        {
            if (dia.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            foreach (var e in empleados)
            {
                var r = rnd.Next(100);
                var estado = r < 3 ? EstadoAsistencia.Falta : r < 10 ? EstadoAsistencia.Tardanza : EstadoAsistencia.Presente;
                var horasExtra = estado != EstadoAsistencia.Falta && r % 9 == 0 ? rnd.Next(1, 5) : 0;
                db.Asistencias.Add(new Asistencia
                {
                    Empleado = e, Fecha = dia, Estado = estado, HorasExtra = horasExtra,
                    HoraEntrada = estado == EstadoAsistencia.Falta ? null : new TimeOnly(estado == EstadoAsistencia.Tardanza ? 8 : 7, estado == EstadoAsistencia.Tardanza ? 20 : 55),
                    HoraSalida = estado == EstadoAsistencia.Falta ? null : new TimeOnly(17 + horasExtra, 0)
                });
            }
        }

        // Solicitudes de vacaciones en distintos estados para mostrar el flujo.
        db.SolicitudesVacaciones.AddRange(
            new SolicitudVacaciones { Empleado = empleados[0], FechaInicio = hoy.AddDays(20), FechaFin = hoy.AddDays(29), Motivo = "Viaje familiar" },
            new SolicitudVacaciones { Empleado = empleados[4], FechaInicio = hoy.AddDays(40), FechaFin = hoy.AddDays(54), Motivo = "Descanso anual" },
            new SolicitudVacaciones { Empleado = empleados[2], FechaInicio = hoy.AddDays(-60), FechaFin = hoy.AddDays(-46), Estado = EstadoSolicitud.Aprobada, ComentarioRevision = "Aprobado. Coordinar reemplazo.", FechaRevision = DateTime.UtcNow.AddDays(-70) },
            new SolicitudVacaciones { Empleado = empleados[5], FechaInicio = hoy.AddDays(5), FechaFin = hoy.AddDays(9), Estado = EstadoSolicitud.Rechazada, ComentarioRevision = "Cierre de inventario esa semana.", FechaRevision = DateTime.UtcNow.AddDays(-2) });

        // Dos planillas históricas CERRADAS (sin asistencia registrada) para que existan boletas que consultar.
        foreach (var meses in new[] { 3, 2 })
        {
            var periodo = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-meses);
            var planilla = new Planilla { Anio = periodo.Year, Mes = periodo.Month, Estado = EstadoPlanilla.Cerrada, FechaCierre = DateTime.UtcNow.AddDays(-30 * meses) };
            foreach (var e in empleados)
            {
                var c = CalculadoraPlanilla.Calcular(new EntradaCalculo(
                    e.SueldoBasico, e.TieneHijos, e.Regimen,
                    e.Afp is null ? null : new TasasAfp(e.Afp.Nombre, e.Afp.AportePct, e.Afp.PrimaSeguroPct, e.Afp.ComisionPct),
                    0, []), parametros);
                planilla.Boletas.Add(new BoletaPago
                {
                    Empleado = e, EmpleadoNombre = e.NombreCompleto, EmpleadoDni = e.Dni, Cargo = e.Cargo!.Nombre, Area = e.Area!.Nombre,
                    Regimen = e.Regimen, AfpNombre = e.Afp?.Nombre, SueldoBasico = c.SueldoBasico, AsignacionFamiliar = c.AsignacionFamiliar,
                    TotalIngresos = c.TotalIngresos, Onp = c.Onp, AfpAporte = c.AfpAporte, AfpPrima = c.AfpPrima, AfpComision = c.AfpComision,
                    Renta5ta = c.Renta5ta, TotalDescuentos = c.TotalDescuentos, NetoPagar = c.NetoPagar, EsSalud = c.EsSalud
                });
            }
            db.Planillas.Add(planilla);
        }

        await db.SaveChangesAsync(ct);
    }
}

using System.Net;
using System.Net.Http.Json;

namespace Planillas.IntegrationTests;

public class VacacionesFlujoTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string Fecha(int dias) => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(dias).ToString("yyyy-MM-dd");

    [Fact]
    public async Task Solicitud_aprobacion_y_reglas_del_flujo()
    {
        var empleado = await factory.ClientAsync(Usuarios.Empleado);
        var rrhh = await factory.ClientAsync(Usuarios.Rrhh);

        var saldoAntes = (await (await empleado.GetAsync("/api/vacaciones/saldo")).JsonAsync()).GetProperty("diasDisponibles").GetInt32();

        // 1. El empleado solicita 5 días
        var creada = await empleado.PostAsJsonAsync("/api/vacaciones", new { fechaInicio = Fecha(60), fechaFin = Fecha(64), motivo = "Viaje" });
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var solicitud = await creada.JsonAsync();
        var id = solicitud.GetProperty("id").GetInt32();
        Assert.Equal("Pendiente", solicitud.GetProperty("estado").GetString());
        Assert.Equal(5, solicitud.GetProperty("dias").GetInt32());

        var saldoDespues = (await (await empleado.GetAsync("/api/vacaciones/saldo")).JsonAsync()).GetProperty("diasDisponibles").GetInt32();
        Assert.Equal(saldoAntes - 5, saldoDespues);

        // 2. Fechas solapadas → 409
        var solapada = await empleado.PostAsJsonAsync("/api/vacaciones", new { fechaInicio = Fecha(62), fechaFin = Fecha(66) });
        Assert.Equal(HttpStatusCode.Conflict, solapada.StatusCode);

        // 3. El empleado no puede aprobarse a sí mismo → 403
        Assert.Equal(HttpStatusCode.Forbidden, (await empleado.PostAsJsonAsync($"/api/vacaciones/{id}/aprobar", new { })).StatusCode);

        // 4. RR.HH. aprueba
        var aprobada = await (await rrhh.PostAsJsonAsync($"/api/vacaciones/{id}/aprobar", new { comentario = "Buen viaje" })).JsonAsync();
        Assert.Equal("Aprobada", aprobada.GetProperty("estado").GetString());

        // 5. Una solicitud ya aprobada no puede volver a procesarse ni cancelarse → 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await rrhh.PostAsJsonAsync($"/api/vacaciones/{id}/rechazar", new { comentario = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await empleado.PostAsync($"/api/vacaciones/{id}/cancelar", null)).StatusCode);
    }

    [Fact]
    public async Task Empleado_solo_ve_sus_solicitudes()
    {
        var empleado = await factory.ClientAsync(Usuarios.Empleado);
        var miId = (await (await empleado.GetAsync("/api/perfil")).JsonAsync()).GetProperty("id").GetInt32();

        // Aunque pida las de otro empleado, el backend fuerza su propio id.
        var lista = await (await empleado.GetAsync("/api/vacaciones?empleadoId=5")).JsonAsync();
        Assert.All(lista.GetProperty("items").EnumerateArray(), s => Assert.Equal(miId, s.GetProperty("empleadoId").GetInt32()));
    }

    [Fact]
    public async Task Rechazo_sin_motivo_devuelve_422()
    {
        var rrhh = await factory.ClientAsync(Usuarios.Rrhh);
        var pendientes = await (await rrhh.GetAsync("/api/vacaciones?estado=Pendiente")).JsonAsync();
        var id = pendientes.GetProperty("items")[0].GetProperty("id").GetInt32();

        var response = await rrhh.PostAsJsonAsync($"/api/vacaciones/{id}/rechazar", new { comentario = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class PlanillaFlujoTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Generar_cerrar_y_publicar_planilla_del_mes_anterior()
    {
        var rrhh = await factory.ClientAsync(Usuarios.Rrhh);
        var empleado = await factory.ClientAsync(Usuarios.Empleado);
        var boletasAntes = (await (await empleado.GetAsync("/api/boletas/mias")).JsonAsync()).GetArrayLength();

        var hoy = DateTime.UtcNow.AddHours(-5);
        var periodo = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-1);

        // 1. Generar: una boleta por empleado activo, con asistencia consolidada
        var generada = await rrhh.PostAsJsonAsync("/api/planillas/generar", new { anio = periodo.Year, mes = periodo.Month });
        Assert.Equal(HttpStatusCode.OK, generada.StatusCode);
        var detalle = await generada.JsonAsync();
        var planilla = detalle.GetProperty("planilla");
        var id = planilla.GetProperty("id").GetInt32();
        Assert.Equal("Borrador", planilla.GetProperty("estado").GetString());
        Assert.Equal(12, planilla.GetProperty("numeroBoletas").GetInt32());

        foreach (var b in detalle.GetProperty("boletas").EnumerateArray())
        {
            var ingresos = b.GetProperty("totalIngresos").GetDecimal();
            var descuentos = b.GetProperty("totalDescuentos").GetDecimal();
            Assert.Equal(ingresos - descuentos, b.GetProperty("netoPagar").GetDecimal());
        }

        // 2. Un borrador NO es visible para el empleado
        Assert.Equal(boletasAntes, (await (await empleado.GetAsync("/api/boletas/mias")).JsonAsync()).GetArrayLength());

        // 3. Regenerar un borrador está permitido
        Assert.Equal(HttpStatusCode.OK, (await rrhh.PostAsJsonAsync("/api/planillas/generar", new { anio = periodo.Year, mes = periodo.Month })).StatusCode);

        // 4. Cerrar
        var cerrada = await (await rrhh.PostAsync($"/api/planillas/{id}/cerrar", null)).JsonAsync();
        Assert.Equal("Cerrada", cerrada.GetProperty("estado").GetString());

        // 5. Una planilla cerrada es inmutable → 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await rrhh.PostAsJsonAsync("/api/planillas/generar", new { anio = periodo.Year, mes = periodo.Month })).StatusCode);

        // 6. Ahora el empleado sí ve su nueva boleta
        Assert.Equal(boletasAntes + 1, (await (await empleado.GetAsync("/api/boletas/mias")).JsonAsync()).GetArrayLength());
    }

    [Fact]
    public async Task No_se_genera_planilla_de_un_periodo_futuro()
    {
        var rrhh = await factory.ClientAsync(Usuarios.Rrhh);
        var futuro = DateTime.UtcNow.AddMonths(3);
        var response = await rrhh.PostAsJsonAsync("/api/planillas/generar", new { anio = futuro.Year, mes = futuro.Month });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Simulador_aplica_las_reglas_de_calculo()
    {
        var admin = await factory.ClientAsync(Usuarios.Admin);
        var r = await (await admin.PostAsJsonAsync("/api/planillas/simular", new
        {
            sueldoBasico = 10000, tieneHijos = false, regimen = "Onp", afpId = (int?)null, diasFalta = 0, horasExtraPorDia = Array.Empty<decimal>()
        })).JsonAsync();

        Assert.Equal(1300m, r.GetProperty("onp").GetDecimal());
        Assert.Equal(1062.67m, r.GetProperty("renta5ta").GetDecimal());
        Assert.Equal(900m, r.GetProperty("esSalud").GetDecimal());
    }

    [Fact]
    public async Task Dashboard_devuelve_kpis()
    {
        var admin = await factory.ClientAsync(Usuarios.Admin);
        var d = await (await admin.GetAsync("/api/dashboard")).JsonAsync();
        Assert.True(d.GetProperty("empleadosActivos").GetInt32() >= 12);
        Assert.True(d.GetProperty("historico").GetArrayLength() >= 2);
    }
}

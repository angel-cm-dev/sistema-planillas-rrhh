using System.Net;
using System.Net.Http.Json;

namespace Planillas.IntegrationTests;

public class AuthYAutorizacionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_responde_healthy()
    {
        var client = await factory.ClientAsync();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", (await response.JsonAsync()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Documento_openapi_incluye_endpoints_y_seguridad_bearer()
    {
        var client = await factory.ClientAsync();
        var doc = await (await client.GetAsync("/openapi/v1.json")).JsonAsync();

        Assert.True(doc.GetProperty("paths").TryGetProperty("/api/planillas/generar", out _));
        Assert.True(doc.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task Login_con_credenciales_demo_devuelve_token_y_rol()
    {
        var client = await factory.ClientAsync();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = Usuarios.Admin, password = "Demo123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.JsonAsync();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.Equal("Admin", body.GetProperty("usuario").GetProperty("rol").GetString());
    }

    [Fact]
    public async Task Login_con_password_incorrecto_devuelve_401_problem_details()
    {
        var client = await factory.ClientAsync();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = Usuarios.Admin, password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(401, (await response.JsonAsync()).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Login_con_email_invalido_devuelve_400_con_errores_por_campo()
    {
        var client = await factory.ClientAsync();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "no-es-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.JsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Endpoint_protegido_sin_token_devuelve_401()
    {
        var client = await factory.ClientAsync();
        var response = await client.GetAsync("/api/empleados");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Empleado_no_puede_listar_empleados()
    {
        var client = await factory.ClientAsync(Usuarios.Empleado);
        var response = await client.GetAsync("/api/empleados");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Rrhh_no_puede_modificar_parametros_legales()
    {
        var client = await factory.ClientAsync(Usuarios.Rrhh);
        var response = await client.PutAsJsonAsync("/api/catalogos/parametros", new
        {
            uit = 5350, rmv = 1130, asignacionFamiliarPct = 10, onpPct = 13, esSaludPct = 9,
            horaExtra25Pct = 25, horaExtra35Pct = 35, topePrimaAfp = 12234.34, horasJornadaDiaria = 8
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Empleado_ve_su_perfil_y_solo_sus_boletas()
    {
        var client = await factory.ClientAsync(Usuarios.Empleado);

        var perfil = await (await client.GetAsync("/api/perfil")).JsonAsync();
        var miId = perfil.GetProperty("id").GetInt32();

        var boletas = await (await client.GetAsync("/api/boletas/mias")).JsonAsync();
        Assert.True(boletas.GetArrayLength() >= 2); // el seed crea dos planillas cerradas
        foreach (var b in boletas.EnumerateArray())
            Assert.Equal(miId, b.GetProperty("empleadoId").GetInt32());

        var propia = await client.GetAsync($"/api/boletas/{boletas[0].GetProperty("id").GetInt32()}");
        Assert.Equal(HttpStatusCode.OK, propia.StatusCode);
    }

    [Fact]
    public async Task Empleado_no_puede_ver_la_boleta_de_otro()
    {
        var rrhh = await factory.ClientAsync(Usuarios.Rrhh);
        var planillas = await (await rrhh.GetAsync("/api/planillas")).JsonAsync();
        var planillaId = planillas.GetProperty("items")[0].GetProperty("id").GetInt32();
        var detalle = await (await rrhh.GetAsync($"/api/planillas/{planillaId}")).JsonAsync();

        var empleado = await factory.ClientAsync(Usuarios.Empleado);
        var miId = (await (await empleado.GetAsync("/api/perfil")).JsonAsync()).GetProperty("id").GetInt32();
        var ajena = detalle.GetProperty("boletas").EnumerateArray().First(b => b.GetProperty("empleadoId").GetInt32() != miId);

        var response = await empleado.GetAsync($"/api/boletas/{ajena.GetProperty("id").GetInt32()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

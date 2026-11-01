using System.Net;
using System.Net.Http.Json;

namespace Planillas.IntegrationTests;

public class EmpleadosTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object NuevoEmpleado(string dni, string regimen = "Onp", int? afpId = null) => new
    {
        dni, nombres = "Valeria", apellidos = "Prueba Integración", email = $"valeria.{dni}@empresa-demo.pe",
        fechaIngreso = "2024-03-01", areaId = 1, cargoId = 1, sueldoBasico = 2500, tieneHijos = false,
        regimen, afpId, cuspp = (string?)null
    };

    [Fact]
    public async Task Listado_es_paginado_y_filtrable()
    {
        var client = await factory.ClientAsync(Usuarios.Rrhh);

        var pagina = await (await client.GetAsync("/api/empleados?page=1&pageSize=5")).JsonAsync();
        Assert.Equal(5, pagina.GetProperty("items").GetArrayLength());
        Assert.True(pagina.GetProperty("totalCount").GetInt32() >= 12);
        Assert.True(pagina.GetProperty("totalPages").GetInt32() >= 3);

        var busqueda = await (await client.GetAsync("/api/empleados?search=quispe")).JsonAsync();
        Assert.Equal(1, busqueda.GetProperty("totalCount").GetInt32());

        var onp = await (await client.GetAsync("/api/empleados?regimen=Onp&pageSize=100")).JsonAsync();
        Assert.All(onp.GetProperty("items").EnumerateArray(), e => Assert.Equal("Onp", e.GetProperty("regimen").GetString()));
    }

    [Fact]
    public async Task Ordena_por_sueldo_descendente()
    {
        var client = await factory.ClientAsync(Usuarios.Admin);
        var pagina = await (await client.GetAsync("/api/empleados?sortBy=sueldo&sortDir=desc&pageSize=100")).JsonAsync();
        var sueldos = pagina.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("sueldoBasico").GetDecimal()).ToList();
        Assert.Equal(sueldos.OrderByDescending(s => s), sueldos);
    }

    [Fact]
    public async Task Crear_con_datos_invalidos_devuelve_400()
    {
        var client = await factory.ClientAsync(Usuarios.Rrhh);
        var response = await client.PostAsJsonAsync("/api/empleados", NuevoEmpleado("123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.JsonAsync()).GetProperty("errors").TryGetProperty("dni", out _));
    }

    [Fact]
    public async Task Afp_sin_afp_asignada_devuelve_400()
    {
        var client = await factory.ClientAsync(Usuarios.Rrhh);
        var response = await client.PostAsJsonAsync("/api/empleados", NuevoEmpleado("80000002", "Afp"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Crear_actualizar_y_desactivar_empleado()
    {
        var client = await factory.ClientAsync(Usuarios.Rrhh);

        var creado = await client.PostAsJsonAsync("/api/empleados", NuevoEmpleado("80000001", "Afp", 1));
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var id = (await creado.JsonAsync()).GetProperty("id").GetInt32();

        var duplicado = await client.PostAsJsonAsync("/api/empleados", NuevoEmpleado("80000001"));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var actualizado = await client.PutAsJsonAsync($"/api/empleados/{id}", NuevoEmpleado("80000001"));
        var body = await actualizado.JsonAsync();
        Assert.Equal("Onp", body.GetProperty("regimen").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, body.GetProperty("afpId").ValueKind); // ONP limpia la AFP

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/empleados/{id}")).StatusCode);
        var final = await (await client.GetAsync($"/api/empleados/{id}")).JsonAsync();
        Assert.False(final.GetProperty("activo").GetBoolean());
    }

    [Fact]
    public async Task Empleado_inexistente_devuelve_404()
    {
        var client = await factory.ClientAsync(Usuarios.Admin);
        var response = await client.GetAsync("/api/empleados/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

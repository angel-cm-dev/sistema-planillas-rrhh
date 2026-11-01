using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Planillas.IntegrationTests;

/// <summary>
/// Levanta la API completa en memoria con una BD SQLite en memoria (sin Docker ni SQL Server).
/// Cada instancia usa su propia BD con nombre único; una conexión abierta la mantiene viva
/// mientras dure la clase de pruebas (SQLite borra la BD en memoria al cerrar la última conexión).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=planillas-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public ApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:Default"] = _connectionString,
            ["Logging:LogLevel:Default"] = "Warning"
        }));
    }

    public async Task<HttpClient> ClientAsync(string? email = null)
    {
        var client = CreateClient();
        if (email is null) return client;
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Demo123!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}

public static class Usuarios
{
    public const string Admin = "admin@demo.pe";
    public const string Rrhh = "rrhh@demo.pe";
    public const string Empleado = "empleado@demo.pe";
}

public static class HttpExtensions
{
    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}

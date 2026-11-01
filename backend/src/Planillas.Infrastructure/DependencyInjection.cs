using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planillas.Application.Common;
using Planillas.Infrastructure.Persistence;
using Planillas.Infrastructure.Security;

namespace Planillas.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra persistencia y seguridad. El proveedor de BD se elige por configuración
    /// (<c>Database:Provider</c> = SqlServer | Sqlite) para correr pruebas y desarrollo local sin Docker.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // La configuración se lee al resolver el DbContext (no al registrarlo) para que las pruebas
        // de integración puedan sobrescribir proveedor y cadena de conexión.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var provider = cfg["Database:Provider"] ?? "SqlServer";
            var connectionString = cfg.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5));
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<DbInitializer>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IClock, LimaClock>();
        return services;
    }
}

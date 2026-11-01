using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Planillas.Application.Features.Asistencias;
using Planillas.Application.Features.Auth;
using Planillas.Application.Features.Catalogos;
using Planillas.Application.Features.Contratos;
using Planillas.Application.Features.Dashboard;
using Planillas.Application.Features.Empleados;
using Planillas.Application.Features.Planillas;
using Planillas.Application.Features.Vacaciones;

namespace Planillas.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso (servicios de aplicación) y todos los validadores del ensamblado.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AuthService>();
        services.AddScoped<EmpleadoService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<ContratoService>();
        services.AddScoped<AsistenciaService>();
        services.AddScoped<VacacionService>();
        services.AddScoped<PlanillaService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}

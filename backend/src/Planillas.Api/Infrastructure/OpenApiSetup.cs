using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Planillas.Api.Infrastructure;

/// <summary>Añade la información del API y el esquema de seguridad Bearer al documento OpenAPI.</summary>
public sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Sistema de Planillas y RR.HH. — API",
            Version = "v1",
            Description = "API REST para gestión de empleados, contratos, asistencia, vacaciones y planilla mensual peruana (cálculo didáctico; tasas referenciales y configurables)."
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Obtenga un token en POST /api/auth/login y péguelo aquí."
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        return Task.CompletedTask;
    }
}

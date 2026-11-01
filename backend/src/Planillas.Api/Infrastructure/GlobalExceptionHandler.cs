using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Planillas.Application.Common;
using Planillas.Domain.Common;

namespace Planillas.Api.Infrastructure;

/// <summary>
/// Traduce excepciones a respuestas RFC 7807 (ProblemDetails) consistentes. Los controladores no
/// tienen try/catch: lanzan excepciones semánticas y este handler decide el código HTTP.
/// </summary>
public class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Datos de entrada inválidos"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autenticado"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        if (status >= 500)
            logger.LogError(exception, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogInformation("Petición rechazada ({Status}) en {Path}: {Message}", status, context.Request.Path, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // Nunca exponemos detalles internos (stack trace, SQL) en errores 500.
            Detail = status >= 500 ? "Ocurrió un error inesperado. Intente nuevamente." : exception.Message,
            Instance = context.Request.Path
        };
        if (exception is ValidationException ve)
        {
            problem.Detail = "Uno o más campos no son válidos.";
            problem.Extensions["errors"] = ve.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static string ToCamelCase(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}

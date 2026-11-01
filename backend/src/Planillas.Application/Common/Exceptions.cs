namespace Planillas.Application.Common;

/// <summary>Recurso inexistente → HTTP 404.</summary>
public class NotFoundException(string recurso, object id)
    : Exception($"{recurso} con id '{id}' no existe.");

/// <summary>Conflicto con el estado actual (duplicados, periodos ya generados) → HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>El usuario está autenticado pero no puede acceder a ESTE recurso concreto → HTTP 403.</summary>
public class ForbiddenException(string message = "No tiene permiso para acceder a este recurso.") : Exception(message);

/// <summary>Credenciales inválidas → HTTP 401.</summary>
public class UnauthorizedException(string message = "Credenciales inválidas.") : Exception(message);

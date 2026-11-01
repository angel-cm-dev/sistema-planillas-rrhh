namespace Planillas.Domain.Common;

/// <summary>Clase base para entidades con identidad numérica y fecha de creación.</summary>
public abstract class Entity
{
    public int Id { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Se lanza cuando se viola una regla de negocio (p. ej. aprobar una solicitud ya rechazada).
/// La capa API la traduce a HTTP 422 (Unprocessable Entity).
/// </summary>
public class DomainException(string message) : Exception(message);

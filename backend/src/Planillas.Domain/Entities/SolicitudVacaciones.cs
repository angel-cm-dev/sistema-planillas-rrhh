using Planillas.Domain.Common;
using Planillas.Domain.Enums;

namespace Planillas.Domain.Entities;

/// <summary>
/// Solicitud de vacaciones con un flujo de estados explícito:
/// Pendiente → Aprobada | Rechazada | Cancelada. Ninguna otra transición es válida.
/// </summary>
public class SolicitudVacaciones : Entity
{
    /// <summary>D.Leg. 713: 30 días calendario de descanso por cada año completo de servicios.</summary>
    public const int DiasPorAnio = 30;

    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string? Motivo { get; set; }
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    public int? RevisadoPorId { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? ComentarioRevision { get; set; }

    /// <summary>Días calendario (inclusive). En Perú las vacaciones se cuentan en días calendario, no hábiles.</summary>
    public int Dias => FechaFin.DayNumber - FechaInicio.DayNumber + 1;

    /// <summary>Solo las solicitudes vivas consumen saldo y bloquean fechas.</summary>
    public bool ConsumeSaldo => Estado is EstadoSolicitud.Pendiente or EstadoSolicitud.Aprobada;

    public static SolicitudVacaciones Crear(int empleadoId, DateOnly inicio, DateOnly fin, string? motivo, int diasDisponibles)
    {
        if (fin < inicio)
            throw new DomainException("La fecha de fin no puede ser anterior a la fecha de inicio.");
        var solicitud = new SolicitudVacaciones { EmpleadoId = empleadoId, FechaInicio = inicio, FechaFin = fin, Motivo = motivo };
        if (solicitud.Dias > diasDisponibles)
            throw new DomainException($"Los días solicitados ({solicitud.Dias}) exceden el saldo disponible ({diasDisponibles}).");
        return solicitud;
    }

    public bool SeSolapaCon(DateOnly inicio, DateOnly fin) => FechaInicio <= fin && inicio <= FechaFin;

    public void Aprobar(int revisorId, string? comentario) => Revisar(EstadoSolicitud.Aprobada, revisorId, comentario);

    public void Rechazar(int revisorId, string? comentario)
    {
        // Rechazar sin explicación es mala práctica de RR.HH.: exigimos el motivo.
        if (string.IsNullOrWhiteSpace(comentario))
            throw new DomainException("Debe indicar el motivo del rechazo.");
        Revisar(EstadoSolicitud.Rechazada, revisorId, comentario);
    }

    public void Cancelar()
    {
        if (Estado != EstadoSolicitud.Pendiente)
            throw new DomainException("Solo se pueden cancelar solicitudes pendientes.");
        Estado = EstadoSolicitud.Cancelada;
    }

    private void Revisar(EstadoSolicitud nuevoEstado, int revisorId, string? comentario)
    {
        if (Estado != EstadoSolicitud.Pendiente)
            throw new DomainException($"La solicitud ya fue procesada (estado: {Estado}).");
        Estado = nuevoEstado;
        RevisadoPorId = revisorId;
        FechaRevision = DateTime.UtcNow;
        ComentarioRevision = comentario;
    }
}

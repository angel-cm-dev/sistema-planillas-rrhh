namespace Planillas.Domain.Enums;

/// <summary>Roles del sistema. Se guardan como texto para que la BD sea legible.</summary>
public enum Rol { Admin, Rrhh, Empleado }

/// <summary>Sistema previsional peruano: público (ONP) o privado (AFP).</summary>
public enum RegimenPensionario { Onp, Afp }

public enum TipoContrato { Indeterminado, PlazoFijo, TiempoParcial }

public enum EstadoContrato { Vigente, Finalizado }

public enum EstadoAsistencia { Presente, Tardanza, Falta, Licencia, Vacaciones }

public enum EstadoSolicitud { Pendiente, Aprobada, Rechazada, Cancelada }

/// <summary>Una planilla en Borrador puede recalcularse; una Cerrada es inmutable (ya se pagó).</summary>
public enum EstadoPlanilla { Borrador, Cerrada }

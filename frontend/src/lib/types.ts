// Tipos espejo de los DTOs del backend (Planillas.Application).

export type Rol = 'Admin' | 'Rrhh' | 'Empleado';
export type Regimen = 'Onp' | 'Afp';

export interface Usuario {
  id: number;
  email: string;
  nombreCompleto: string;
  rol: Rol;
  empleadoId: number | null;
}

export interface LoginResponse {
  token: string;
  expiraEn: string;
  usuario: Usuario;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Catalogo {
  id: number;
  nombre: string;
}

export interface Afp {
  id: number;
  nombre: string;
  aportePct: number;
  primaSeguroPct: number;
  comisionPct: number;
}

export interface Parametros {
  uit: number;
  rmv: number;
  asignacionFamiliarPct: number;
  onpPct: number;
  esSaludPct: number;
  horaExtra25Pct: number;
  horaExtra35Pct: number;
  topePrimaAfp: number;
  horasJornadaDiaria: number;
}

export interface EmpleadoListItem {
  id: number;
  dni: string;
  nombreCompleto: string;
  email: string;
  area: string;
  cargo: string;
  sueldoBasico: number;
  regimen: Regimen;
  afp: string | null;
  fechaIngreso: string;
  activo: boolean;
}

export interface EmpleadoDetail {
  id: number;
  dni: string;
  nombres: string;
  apellidos: string;
  email: string;
  fechaIngreso: string;
  areaId: number;
  area: string;
  cargoId: number;
  cargo: string;
  sueldoBasico: number;
  tieneHijos: boolean;
  regimen: Regimen;
  afpId: number | null;
  afp: string | null;
  cuspp: string | null;
  activo: boolean;
}

export interface Contrato {
  id: number;
  empleadoId: number;
  empleado: string;
  tipo: 'Indeterminado' | 'PlazoFijo' | 'TiempoParcial';
  fechaInicio: string;
  fechaFin: string | null;
  sueldo: number;
  estado: 'Vigente' | 'Finalizado';
}

export type EstadoAsistencia = 'Presente' | 'Tardanza' | 'Falta' | 'Licencia' | 'Vacaciones';

export interface Asistencia {
  id: number;
  empleadoId: number;
  empleado: string;
  fecha: string;
  estado: EstadoAsistencia;
  horaEntrada: string | null;
  horaSalida: string | null;
  horasExtra: number;
  observacion: string | null;
}

export type EstadoSolicitud = 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Cancelada';

export interface SolicitudVacaciones {
  id: number;
  empleadoId: number;
  empleado: string;
  fechaInicio: string;
  fechaFin: string;
  dias: number;
  estado: EstadoSolicitud;
  motivo: string | null;
  comentarioRevision: string | null;
  fechaRevision: string | null;
  creadoEn: string;
}

export interface SaldoVacaciones {
  empleadoId: number;
  diasGanados: number;
  diasConsumidos: number;
  diasDisponibles: number;
}

export interface PlanillaResumen {
  id: number;
  anio: number;
  mes: number;
  periodo: string;
  estado: 'Borrador' | 'Cerrada';
  numeroBoletas: number;
  totalIngresos: number;
  totalDescuentos: number;
  totalNeto: number;
  totalEsSalud: number;
  creadoEn: string;
  fechaCierre: string | null;
}

export interface BoletaResumen {
  id: number;
  planillaId: number;
  periodo: string;
  estadoPlanilla: string;
  empleadoId: number;
  empleado: string;
  dni: string;
  regimen: Regimen;
  totalIngresos: number;
  totalDescuentos: number;
  netoPagar: number;
  esSalud: number;
}

export interface PlanillaDetalle {
  planilla: PlanillaResumen;
  boletas: BoletaResumen[];
}

export interface Boleta {
  id: number;
  planillaId: number;
  periodo: string;
  estadoPlanilla: string;
  empleadoId: number;
  empleadoNombre: string;
  empleadoDni: string;
  cargo: string;
  area: string;
  regimen: Regimen;
  afpNombre: string | null;
  sueldoBasico: number;
  asignacionFamiliar: number;
  horasExtra25: number;
  horasExtra35: number;
  montoHorasExtra: number;
  diasFalta: number;
  descuentoFaltas: number;
  totalIngresos: number;
  onp: number;
  afpAporte: number;
  afpPrima: number;
  afpComision: number;
  renta5ta: number;
  totalDescuentos: number;
  netoPagar: number;
  esSalud: number;
}

export interface ResultadoCalculo {
  sueldoBasico: number;
  asignacionFamiliar: number;
  valorHora: number;
  horasExtra25: number;
  horasExtra35: number;
  montoHorasExtra: number;
  diasFalta: number;
  descuentoFaltas: number;
  totalIngresos: number;
  onp: number;
  afpAporte: number;
  afpPrima: number;
  afpComision: number;
  renta5ta: number;
  totalDescuentos: number;
  netoPagar: number;
  esSalud: number;
  costoEmpleador: number;
}

export interface Dashboard {
  empleadosActivos: number;
  empleadosInactivos: number;
  masaSalarialMensual: number;
  solicitudesPendientes: number;
  contratosPorVencer: number;
  ultimoPeriodo: string | null;
  ultimoNeto: number;
  ultimoCostoEmpleador: number;
  empleadosPorArea: { etiqueta: string; cantidad: number }[];
  distribucionRegimen: { etiqueta: string; cantidad: number }[];
  historico: { periodo: string; neto: number; costoEmpleador: number }[];
}

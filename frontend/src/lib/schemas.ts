import { z } from 'zod';

// Esquemas de validación del lado cliente. Replican las reglas del backend (FluentValidation)
// para dar feedback inmediato, pero el backend sigue siendo la fuente de verdad.

const numero = (msg: string) =>
  z.coerce.number({ invalid_type_error: msg });

export const empleadoSchema = z
  .object({
    dni: z.string().regex(/^\d{8}$/, 'El DNI debe tener 8 dígitos'),
    nombres: z.string().trim().min(1, 'Requerido').max(100),
    apellidos: z.string().trim().min(1, 'Requerido').max(100),
    email: z.string().email('Correo inválido'),
    fechaIngreso: z.string().min(1, 'Requerido'),
    areaId: numero('Seleccione un área').int().positive('Seleccione un área'),
    cargoId: numero('Seleccione un cargo').int().positive('Seleccione un cargo'),
    sueldoBasico: numero('Ingrese un monto').positive('Debe ser mayor a 0').max(999999),
    tieneHijos: z.boolean(),
    regimen: z.enum(['Onp', 'Afp']),
    afpId: z.coerce.number().optional().nullable(),
    cuspp: z.string().max(12).optional().nullable(),
  })
  .refine((v) => v.regimen === 'Onp' || (v.afpId != null && v.afpId > 0), {
    message: 'Seleccione una AFP para el régimen privado',
    path: ['afpId'],
  });

export type EmpleadoForm = z.infer<typeof empleadoSchema>;

export const solicitudSchema = z
  .object({
    empleadoId: z.coerce.number().optional().nullable(),
    fechaInicio: z.string().min(1, 'Requerido'),
    fechaFin: z.string().min(1, 'Requerido'),
    motivo: z.string().max(250).optional(),
  })
  .refine((v) => !v.fechaInicio || !v.fechaFin || v.fechaFin >= v.fechaInicio, {
    message: 'La fecha de fin no puede ser anterior al inicio',
    path: ['fechaFin'],
  });

export type SolicitudForm = z.infer<typeof solicitudSchema>;

export const asistenciaSchema = z
  .object({
    empleadoId: numero('Seleccione un empleado').int().positive('Seleccione un empleado'),
    fecha: z.string().min(1, 'Requerido'),
    estado: z.enum(['Presente', 'Tardanza', 'Falta', 'Licencia', 'Vacaciones']),
    horaEntrada: z.string().optional(),
    horaSalida: z.string().optional(),
    horasExtra: numero('Ingrese un número').min(0, 'Mínimo 0').max(12, 'Máximo 12'),
    observacion: z.string().max(250).optional(),
  })
  .refine((v) => !(v.estado === 'Falta' && v.horasExtra > 0), {
    message: 'Un día de falta no puede tener horas extra',
    path: ['horasExtra'],
  });

export type AsistenciaForm = z.infer<typeof asistenciaSchema>;

export const contratoSchema = z
  .object({
    empleadoId: numero('Seleccione un empleado').int().positive('Seleccione un empleado'),
    tipo: z.enum(['Indeterminado', 'PlazoFijo', 'TiempoParcial']),
    fechaInicio: z.string().min(1, 'Requerido'),
    fechaFin: z.string().optional(),
    sueldo: numero('Ingrese un monto').positive('Debe ser mayor a 0'),
  })
  .refine((v) => v.tipo !== 'PlazoFijo' || !!v.fechaFin, {
    message: 'Un contrato a plazo fijo requiere fecha de fin',
    path: ['fechaFin'],
  });

export type ContratoForm = z.infer<typeof contratoSchema>;

import { asistenciaSchema, contratoSchema, empleadoSchema, solicitudSchema } from './schemas';

const empleadoValido = {
  dni: '12345678', nombres: 'Ana', apellidos: 'Pérez', email: 'ana@demo.pe', fechaIngreso: '2024-01-01',
  areaId: '1', cargoId: '2', sueldoBasico: '2500', tieneHijos: false, regimen: 'Onp' as const,
};

describe('empleadoSchema', () => {
  it('acepta un empleado ONP válido (y convierte números)', () => {
    const r = empleadoSchema.safeParse(empleadoValido);
    expect(r.success).toBe(true);
    if (r.success) expect(r.data.sueldoBasico).toBe(2500);
  });

  it('rechaza DNI que no tiene 8 dígitos', () => {
    expect(empleadoSchema.safeParse({ ...empleadoValido, dni: '1234' }).success).toBe(false);
  });

  it('exige AFP cuando el régimen es privado', () => {
    const r = empleadoSchema.safeParse({ ...empleadoValido, regimen: 'Afp', afpId: '' });
    expect(r.success).toBe(false);
    if (!r.success) expect(r.error.issues[0].path).toEqual(['afpId']);
  });
});

describe('otros esquemas', () => {
  it('solicitud: el fin no puede ser anterior al inicio', () => {
    expect(solicitudSchema.safeParse({ fechaInicio: '2026-12-10', fechaFin: '2026-12-01' }).success).toBe(false);
    expect(solicitudSchema.safeParse({ fechaInicio: '2026-12-01', fechaFin: '2026-12-10' }).success).toBe(true);
  });

  it('asistencia: una falta no admite horas extra', () => {
    const base = { empleadoId: 1, fecha: '2026-10-01', estado: 'Falta' as const, horasExtra: 2 };
    expect(asistenciaSchema.safeParse(base).success).toBe(false);
    expect(asistenciaSchema.safeParse({ ...base, horasExtra: 0 }).success).toBe(true);
  });

  it('contrato a plazo fijo requiere fecha de fin', () => {
    const base = { empleadoId: 1, tipo: 'PlazoFijo' as const, fechaInicio: '2026-01-01', sueldo: 2000 };
    expect(contratoSchema.safeParse(base).success).toBe(false);
    expect(contratoSchema.safeParse({ ...base, fechaFin: '2026-12-31' }).success).toBe(true);
  });
});

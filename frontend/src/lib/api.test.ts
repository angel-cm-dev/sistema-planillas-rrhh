import { api, ApiError, toApiError, toQuery, tokenStorage } from './api';

describe('toQuery', () => {
  it('omite valores vacíos y codifica el resto', () => {
    expect(toQuery({ page: 1, search: 'ana maría', areaId: '', regimen: undefined, activo: false }))
      .toBe('?page=1&search=ana+mar%C3%ADa&activo=false');
  });

  it('devuelve cadena vacía sin parámetros', () => {
    expect(toQuery({})).toBe('');
  });
});

describe('toApiError', () => {
  it('usa el detail de ProblemDetails y conserva los errores por campo', () => {
    const err = toApiError(400, {
      title: 'Datos inválidos',
      detail: 'Uno o más campos no son válidos.',
      errors: { dni: ['El DNI debe tener 8 dígitos.'] },
    });
    expect(err).toBeInstanceOf(ApiError);
    expect(err.status).toBe(400);
    expect(err.message).toBe('Uno o más campos no son válidos.');
    expect(err.fieldErrors.dni[0]).toContain('8 dígitos');
  });

  it('usa un mensaje por defecto si no hay cuerpo', () => {
    expect(toApiError(403, null).message).toMatch(/permisos/);
    expect(toApiError(0, undefined).message).toMatch(/conectar/);
  });
});

describe('api client', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    tokenStorage.clear();
  });

  it('envía el token Bearer y parsea JSON', async () => {
    tokenStorage.set('abc');
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200 }));

    const data = await api.get<{ ok: boolean }>('/api/x');

    expect(data.ok).toBe(true);
    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer abc');
  });

  it('lanza ApiError con el detalle del servidor', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 409, detail: 'Ya existe un empleado con DNI 1.' }), { status: 409 }),
    );
    await expect(api.post('/api/empleados', {})).rejects.toMatchObject({ status: 409, message: 'Ya existe un empleado con DNI 1.' });
  });

  it('convierte fallos de red en ApiError con status 0', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'));
    await expect(api.get('/api/x')).rejects.toMatchObject({ status: 0 });
  });
});

// Cliente HTTP mínimo sobre fetch: añade el JWT, serializa JSON y convierte las respuestas
// ProblemDetails del backend en un ApiError tipado que la UI puede mostrar.

const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? '';
const TOKEN_KEY = 'planillas.token';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    /** Errores de validación por campo (ProblemDetails.errors). */
    public readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export const tokenStorage = {
  get: (): string | null => {
    try { return localStorage.getItem(TOKEN_KEY); } catch { return null; }
  },
  set: (token: string) => {
    try { localStorage.setItem(TOKEN_KEY, token); } catch { /* almacenamiento no disponible */ }
  },
  clear: () => {
    try { localStorage.removeItem(TOKEN_KEY); } catch { /* almacenamiento no disponible */ }
  },
};

/** Se invoca cuando el backend responde 401 (token vencido): el AuthProvider cierra la sesión. */
let onUnauthorized: (() => void) | null = null;
export const setUnauthorizedHandler = (handler: (() => void) | null) => { onUnauthorized = handler; };

/** Convierte un cuerpo ProblemDetails (o cualquier error) en ApiError. Exportada para pruebas. */
export function toApiError(status: number, body: unknown): ApiError {
  const problem = (body && typeof body === 'object' ? body : {}) as {
    title?: string; detail?: string; errors?: Record<string, string[]>;
  };
  const fallback: Record<number, string> = {
    0: 'No se pudo conectar con el servidor.',
    401: 'Su sesión expiró. Inicie sesión nuevamente.',
    403: 'No tiene permisos para realizar esta acción.',
    404: 'El recurso solicitado no existe.',
    500: 'Error interno del servidor.',
  };
  const message = problem.detail ?? problem.title ?? fallback[status] ?? `Error ${status}`;
  return new ApiError(status, message, problem.errors ?? {});
}

/** Construye un query string omitiendo valores vacíos. */
export function toQuery(params: Record<string, string | number | boolean | null | undefined>): string {
  const qs = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') qs.append(k, String(v));
  });
  const s = qs.toString();
  return s ? `?${s}` : '';
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const token = tokenStorage.get();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response: Response;
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw toApiError(0, null);
  }

  if (response.status === 204) return undefined as T;
  const text = await response.text();
  const data = text ? safeJson(text) : null;

  if (!response.ok) {
    // En login un 401 significa "credenciales inválidas", no "sesión expirada".
    if (response.status === 401 && token && onUnauthorized) onUnauthorized();
    throw toApiError(response.status, data);
  }
  return data as T;
}

function safeJson(text: string): unknown {
  try { return JSON.parse(text); } catch { return null; }
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body ?? {}),
  put: <T>(path: string, body: unknown) => request<T>('PUT', path, body),
  del: (path: string) => request<void>('DELETE', path),
};

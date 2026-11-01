import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from './api';

interface AsyncState<T> {
  data: T | null;
  error: ApiError | null;
  loading: boolean;
  reload: () => void;
}

/**
 * Carga datos asíncronos y expone los tres estados que toda pantalla debe manejar:
 * cargando, error y datos. Se vuelve a ejecutar cuando cambian las dependencias.
 * Ignora respuestas de peticiones antiguas (evita "parpadeos" al tipear en un buscador).
 */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [loading, setLoading] = useState(true);
  const [tick, setTick] = useState(0);
  const requestId = useRef(0);

  useEffect(() => {
    const id = ++requestId.current;
    setLoading(true);
    setError(null);
    fn()
      .then((d) => { if (id === requestId.current) setData(d); })
      .catch((e: unknown) => {
        if (id === requestId.current) setError(e instanceof ApiError ? e : new ApiError(0, String(e)));
      })
      .finally(() => { if (id === requestId.current) setLoading(false); });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick]);

  const reload = useCallback(() => setTick((t) => t + 1), []);
  return { data, error, loading, reload };
}

/** Retrasa la actualización de un valor (p. ej. el texto de búsqueda) para no saturar la API. */
export function useDebounce<T>(value: T, delay = 350): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return debounced;
}

/** Tema claro/oscuro persistido en localStorage (clase "dark" en <html>, estrategia de Tailwind). */
export function useTheme() {
  const [dark, setDark] = useState(() => document.documentElement.classList.contains('dark'));
  const toggle = useCallback(() => {
    setDark((prev) => {
      const next = !prev;
      document.documentElement.classList.toggle('dark', next);
      try { localStorage.setItem('theme', next ? 'dark' : 'light'); } catch { /* sin almacenamiento */ }
      return next;
    });
  }, []);
  return { dark, toggle };
}

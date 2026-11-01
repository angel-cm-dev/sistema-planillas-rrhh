// Utilidades de formato con convenciones peruanas (es-PE, soles).

const soles = new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN', minimumFractionDigits: 2 });
const numero = new Intl.NumberFormat('es-PE', { maximumFractionDigits: 2 });

export const formatCurrency = (value: number | null | undefined): string => soles.format(value ?? 0);

export const formatNumber = (value: number | null | undefined): string => numero.format(value ?? 0);

export const MESES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

/** "2026-10" → "Octubre 2026". */
export function formatPeriodo(periodo: string): string {
  const [anio, mes] = periodo.split('-').map(Number);
  if (!anio || !mes || mes < 1 || mes > 12) return periodo;
  return `${MESES[mes - 1]} ${anio}`;
}

/**
 * "2026-10-05" → "05/10/2026". Se parsea manualmente: new Date("2026-10-05") se interpreta
 * en UTC y, en Lima (UTC-5), mostraría el día anterior.
 */
export function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  const [y, m, d] = iso.slice(0, 10).split('-');
  return y && m && d ? `${d}/${m}/${y}` : iso;
}

/** Fecha local en formato ISO (yyyy-mm-dd) para inputs type="date". */
export function todayIso(offsetDays = 0): string {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export const REGIMEN_LABEL: Record<string, string> = { Onp: 'ONP', Afp: 'AFP' };

export const TIPO_CONTRATO_LABEL: Record<string, string> = {
  Indeterminado: 'Plazo indeterminado',
  PlazoFijo: 'Plazo fijo',
  TiempoParcial: 'Tiempo parcial',
};

/** Rango de páginas a mostrar en la paginación, con "…" para huecos. */
export function pageRange(current: number, total: number, window = 1): (number | '…')[] {
  if (total <= 1) return [1];
  const pages = new Set<number>([1, total]);
  for (let p = current - window; p <= current + window; p++) if (p >= 1 && p <= total) pages.add(p);
  const sorted = [...pages].sort((a, b) => a - b);
  const result: (number | '…')[] = [];
  sorted.forEach((p, i) => {
    if (i > 0 && p - sorted[i - 1] > 1) result.push('…');
    result.push(p);
  });
  return result;
}

/** "2, 3, 1.5" → [2, 3, 1.5]. Ignora valores no numéricos. */
export function parseHorasExtra(texto: string): number[] {
  return texto
    .split(/[,;\s]+/)
    .map((t) => Number(t.replace(',', '.')))
    .filter((n) => Number.isFinite(n) && n > 0);
}

import type { ReactNode } from 'react';
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight } from 'lucide-react';
import { pageRange } from '@/lib/format';
import { EmptyState, ErrorState, Spinner } from './ui';
import type { ApiError } from '@/lib/api';

export interface Column<T> {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  /** Si se indica, la columna es ordenable por esta clave del backend. */
  sortKey?: string;
  className?: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[] | undefined;
  loading: boolean;
  error: ApiError | null;
  onRetry?: () => void;
  rowKey: (row: T) => string | number;
  emptyMessage?: string;
  sort?: { by: string; dir: 'asc' | 'desc' };
  onSortChange?: (by: string, dir: 'asc' | 'desc') => void;
}

/** Tabla genérica con los estados cargando / error / vacío y cabeceras ordenables. */
export function DataTable<T>({ columns, rows, loading, error, onRetry, rowKey, emptyMessage, sort, onSortChange }: DataTableProps<T>) {
  if (error) return <ErrorState message={error.message} onRetry={onRetry} />;
  if (loading && !rows) return <Spinner />;
  if (!rows || rows.length === 0) return <EmptyState message={emptyMessage ?? 'No hay registros que coincidan con los filtros.'} />;

  const toggleSort = (key: string) => {
    if (!onSortChange) return;
    const dir = sort?.by === key && sort.dir === 'asc' ? 'desc' : 'asc';
    onSortChange(key, dir);
  };

  return (
    <div className={`overflow-x-auto ${loading ? 'opacity-60 transition-opacity' : ''}`}>
      <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
        <thead className="bg-slate-50 dark:bg-slate-900/60">
          <tr>
            {columns.map((c) => (
              <th key={c.key} scope="col" className={`whitespace-nowrap px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-300 ${c.className ?? ''}`}>
                {c.sortKey ? (
                  <button className="inline-flex items-center gap-1 hover:text-brand-600" onClick={() => toggleSort(c.sortKey!)}>
                    {c.header}
                    {sort?.by === c.sortKey && (sort.dir === 'asc' ? <ArrowUp className="h-3.5 w-3.5" /> : <ArrowDown className="h-3.5 w-3.5" />)}
                  </button>
                ) : c.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {rows.map((row) => (
            <tr key={rowKey(row)} className="hover:bg-slate-50/70 dark:hover:bg-slate-800/40">
              {columns.map((c) => (
                <td key={c.key} className={`whitespace-nowrap px-4 py-3 ${c.className ?? ''}`}>{c.render(row)}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

interface PaginationProps {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, totalPages, totalCount, pageSize, onPageChange }: PaginationProps) {
  if (totalCount === 0) return null;
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);
  const btn = 'inline-flex h-8 min-w-8 items-center justify-center rounded-md px-2 text-sm disabled:opacity-40';

  return (
    <nav aria-label="Paginación" className="flex flex-col items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 text-sm dark:border-slate-800 sm:flex-row">
      <p className="text-slate-500 dark:text-slate-400">
        Mostrando <span className="font-medium">{from}</span>–<span className="font-medium">{to}</span> de{' '}
        <span className="font-medium">{totalCount}</span>
      </p>
      <div className="flex items-center gap-1">
        <button className={`${btn} hover:bg-slate-100 dark:hover:bg-slate-800`} disabled={page <= 1} onClick={() => onPageChange(page - 1)} aria-label="Página anterior">
          <ChevronLeft className="h-4 w-4" />
        </button>
        {pageRange(page, totalPages).map((p, i) =>
          p === '…' ? (
            <span key={`gap-${i}`} className="px-1 text-slate-400">…</span>
          ) : (
            <button
              key={p}
              onClick={() => onPageChange(p)}
              aria-current={p === page ? 'page' : undefined}
              className={`${btn} ${p === page ? 'bg-brand-600 text-white' : 'hover:bg-slate-100 dark:hover:bg-slate-800'}`}
            >
              {p}
            </button>
          ),
        )}
        <button className={`${btn} hover:bg-slate-100 dark:hover:bg-slate-800`} disabled={page >= totalPages} onClick={() => onPageChange(page + 1)} aria-label="Página siguiente">
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </nav>
  );
}

import { Link } from 'react-router-dom';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { useAsync } from '@/lib/hooks';
import { formatCurrency, formatPeriodo } from '@/lib/format';
import type { BoletaResumen } from '@/lib/types';
import { DataTable, type Column } from '@/components/DataTable';
import { Alert, Card, PageHeader } from '@/components/ui';

export default function MisBoletasPage() {
  const { user } = useAuth();
  const { data, error, loading, reload } = useAsync(
    () => (user?.empleadoId ? api.get<BoletaResumen[]>('/api/boletas/mias') : Promise.resolve([])),
    [user?.empleadoId],
  );

  const columns: Column<BoletaResumen>[] = [
    { key: 'periodo', header: 'Periodo', render: (b) => <span className="font-medium">{formatPeriodo(b.periodo)}</span> },
    { key: 'ingresos', header: 'Ingresos', className: 'text-right', render: (b) => <span className="tabular-nums">{formatCurrency(b.totalIngresos)}</span> },
    { key: 'descuentos', header: 'Descuentos', className: 'text-right', render: (b) => <span className="tabular-nums">{formatCurrency(b.totalDescuentos)}</span> },
    { key: 'neto', header: 'Neto recibido', className: 'text-right', render: (b) => <span className="font-semibold tabular-nums">{formatCurrency(b.netoPagar)}</span> },
    { key: 'ver', header: '', className: 'text-right', render: (b) => <Link to={`/boletas/${b.id}`} className="text-brand-600 hover:underline">Ver / imprimir</Link> },
  ];

  return (
    <>
      <PageHeader title="Mis boletas" description="Boletas de pago de planillas cerradas." />
      {!user?.empleadoId && <div className="mb-4"><Alert tone="info">Su usuario no está vinculado a una ficha de empleado.</Alert></div>}
      <Card>
        <DataTable columns={columns} rows={data ?? undefined} loading={loading} error={error} onRetry={reload} rowKey={(b) => b.id}
          emptyMessage="Aún no tiene boletas publicadas." />
      </Card>
    </>
  );
}

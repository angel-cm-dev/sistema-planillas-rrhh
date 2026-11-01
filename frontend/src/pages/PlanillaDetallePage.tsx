import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Lock } from 'lucide-react';
import { api, ApiError } from '@/lib/api';
import { useAsync } from '@/lib/hooks';
import { formatCurrency, formatPeriodo, REGIMEN_LABEL } from '@/lib/format';
import type { BoletaResumen, PlanillaDetalle } from '@/lib/types';
import { DataTable, type Column } from '@/components/DataTable';
import { Alert, Badge, Button, Card, ErrorState, estadoTone, PageHeader, Spinner, StatCard } from '@/components/ui';

export default function PlanillaDetallePage() {
  const { id } = useParams();
  const { data, error, loading, reload } = useAsync(() => api.get<PlanillaDetalle>(`/api/planillas/${id}`), [id]);
  const [cerrando, setCerrando] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  if (loading && !data) return <Spinner />;
  if (error) return <ErrorState message={error.message} onRetry={reload} />;
  if (!data) return null;
  const { planilla, boletas } = data;

  const cerrar = async () => {
    if (!confirm('Al cerrar la planilla quedará inmutable y las boletas serán visibles para los empleados. ¿Continuar?')) return;
    setCerrando(true);
    setActionError(null);
    try {
      await api.post(`/api/planillas/${planilla.id}/cerrar`);
      reload();
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : 'No se pudo cerrar');
    } finally {
      setCerrando(false);
    }
  };

  const columns: Column<BoletaResumen>[] = [
    { key: 'empleado', header: 'Empleado', render: (b) => <div><p className="font-medium">{b.empleado}</p><p className="text-xs text-slate-500">DNI {b.dni}</p></div> },
    { key: 'regimen', header: 'Pensión', render: (b) => REGIMEN_LABEL[b.regimen] },
    { key: 'ingresos', header: 'Ingresos', className: 'text-right', render: (b) => <span className="tabular-nums">{formatCurrency(b.totalIngresos)}</span> },
    { key: 'descuentos', header: 'Descuentos', className: 'text-right', render: (b) => <span className="tabular-nums text-rose-600 dark:text-rose-400">-{formatCurrency(b.totalDescuentos)}</span> },
    { key: 'neto', header: 'Neto', className: 'text-right', render: (b) => <span className="font-semibold tabular-nums">{formatCurrency(b.netoPagar)}</span> },
    { key: 'essalud', header: 'EsSalud', className: 'text-right', render: (b) => <span className="tabular-nums text-slate-500">{formatCurrency(b.esSalud)}</span> },
    { key: 'ver', header: '', className: 'text-right', render: (b) => <Link className="text-brand-600 hover:underline" to={`/boletas/${b.id}`}>Ver boleta</Link> },
  ];

  return (
    <>
      <Link to="/planillas" className="mb-3 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-brand-600"><ArrowLeft className="h-4 w-4" /> Planillas</Link>
      <PageHeader
        title={`Planilla ${formatPeriodo(planilla.periodo)}`}
        description={`${planilla.numeroBoletas} boletas`}
        actions={
          <>
            <Badge tone={estadoTone(planilla.estado)}>{planilla.estado}</Badge>
            {planilla.estado === 'Borrador' && <Button onClick={cerrar} loading={cerrando}><Lock className="h-4 w-4" /> Cerrar planilla</Button>}
          </>
        }
      />
      {actionError && <div className="mb-4"><Alert>{actionError}</Alert></div>}
      {planilla.estado === 'Borrador' && (
        <div className="mb-4"><Alert tone="info">Borrador: puede recalcularse desde la pantalla de Planillas. Los empleados aún no ven estas boletas.</Alert></div>
      )}
      <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Total ingresos" value={formatCurrency(planilla.totalIngresos)} />
        <StatCard label="Total descuentos" value={formatCurrency(planilla.totalDescuentos)} hint="ONP/AFP + renta 5ta" />
        <StatCard label="Neto a pagar" value={formatCurrency(planilla.totalNeto)} />
        <StatCard label="Costo empleador" value={formatCurrency(planilla.totalIngresos + planilla.totalEsSalud)} hint={`EsSalud: ${formatCurrency(planilla.totalEsSalud)}`} />
      </div>
      <Card>
        <DataTable columns={columns} rows={boletas} loading={false} error={null} rowKey={(b) => b.id} />
      </Card>
    </>
  );
}

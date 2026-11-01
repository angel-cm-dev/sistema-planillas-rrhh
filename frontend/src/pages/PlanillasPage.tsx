import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Play } from 'lucide-react';
import { api, ApiError, toQuery } from '@/lib/api';
import { useAsync } from '@/lib/hooks';
import { formatCurrency, formatDate, formatPeriodo, MESES } from '@/lib/format';
import type { PagedResult, PlanillaDetalle, PlanillaResumen } from '@/lib/types';
import { DataTable, Pagination, type Column } from '@/components/DataTable';
import { Alert, Badge, Button, Card, estadoTone, Field, Input, PageHeader, Select } from '@/components/ui';

export default function PlanillasPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const { data, error, loading, reload } = useAsync(
    () => api.get<PagedResult<PlanillaResumen>>(`/api/planillas${toQuery({ page, pageSize: 12 })}`),
    [page],
  );

  // Por defecto se propone el mes anterior (la planilla se calcula al cierre del mes).
  const hoy = new Date();
  const anterior = new Date(hoy.getFullYear(), hoy.getMonth() - 1, 1);
  const [anio, setAnio] = useState(anterior.getFullYear());
  const [mes, setMes] = useState(anterior.getMonth() + 1);
  const [generando, setGenerando] = useState(false);
  const [genError, setGenError] = useState<string | null>(null);

  const generar = async (e: FormEvent) => {
    e.preventDefault();
    setGenError(null);
    setGenerando(true);
    try {
      const res = await api.post<PlanillaDetalle>('/api/planillas/generar', { anio, mes });
      navigate(`/planillas/${res.planilla.id}`);
    } catch (err) {
      setGenError(err instanceof ApiError ? err.message : 'No se pudo generar la planilla');
    } finally {
      setGenerando(false);
    }
  };

  const columns: Column<PlanillaResumen>[] = [
    { key: 'periodo', header: 'Periodo', render: (p) => <Link to={`/planillas/${p.id}`} className="font-medium text-brand-600 hover:underline">{formatPeriodo(p.periodo)}</Link> },
    { key: 'estado', header: 'Estado', render: (p) => <Badge tone={estadoTone(p.estado)}>{p.estado}</Badge> },
    { key: 'boletas', header: 'Boletas', className: 'text-right', render: (p) => p.numeroBoletas },
    { key: 'ingresos', header: 'Ingresos', className: 'text-right', render: (p) => <span className="tabular-nums">{formatCurrency(p.totalIngresos)}</span> },
    { key: 'descuentos', header: 'Descuentos', className: 'text-right', render: (p) => <span className="tabular-nums">{formatCurrency(p.totalDescuentos)}</span> },
    { key: 'neto', header: 'Neto a pagar', className: 'text-right', render: (p) => <span className="font-semibold tabular-nums">{formatCurrency(p.totalNeto)}</span> },
    { key: 'essalud', header: 'EsSalud (empleador)', className: 'text-right', render: (p) => <span className="tabular-nums">{formatCurrency(p.totalEsSalud)}</span> },
    { key: 'cierre', header: 'Cierre', render: (p) => (p.fechaCierre ? formatDate(p.fechaCierre) : '—') },
  ];

  return (
    <>
      <PageHeader title="Planillas" description="Genere la planilla mensual, revísela en borrador y ciérrela para publicar las boletas." />
      <Card className="mb-6 p-5">
        <form onSubmit={generar} className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <Field label="Mes" htmlFor="mes">
            <Select id="mes" value={mes} onChange={(e) => setMes(Number(e.target.value))}>
              {MESES.map((m, i) => <option key={m} value={i + 1}>{m}</option>)}
            </Select>
          </Field>
          <Field label="Año" htmlFor="anio">
            <Input id="anio" type="number" min={2000} max={2100} value={anio} onChange={(e) => setAnio(Number(e.target.value))} />
          </Field>
          <Button type="submit" loading={generando}><Play className="h-4 w-4" /> Generar / recalcular</Button>
        </form>
        {genError && <div className="mt-4"><Alert>{genError}</Alert></div>}
        <p className="mt-3 text-xs text-slate-500">
          Si el periodo ya existe en borrador se recalcula con los datos actuales. Una planilla cerrada no puede modificarse.
        </p>
      </Card>
      <Card>
        <DataTable columns={columns} rows={data?.items} loading={loading} error={error} onRetry={reload} rowKey={(p) => p.id} emptyMessage="Aún no se ha generado ninguna planilla." />
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
    </>
  );
}

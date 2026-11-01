import { Link } from 'react-router-dom';
import { AlarmClock, Banknote, Building2, Palmtree, Users } from 'lucide-react';
import { api } from '@/lib/api';
import { useAsync } from '@/lib/hooks';
import { formatCurrency, formatPeriodo } from '@/lib/format';
import type { Dashboard } from '@/lib/types';
import { Card, ErrorState, PageHeader, Spinner, StatCard } from '@/components/ui';

export default function DashboardPage() {
  const { data, error, loading, reload } = useAsync(() => api.get<Dashboard>('/api/dashboard'), []);

  if (loading && !data) return <Spinner />;
  if (error) return <ErrorState message={error.message} onRetry={reload} />;
  if (!data) return null;

  const maxArea = Math.max(1, ...data.empleadosPorArea.map((a) => a.cantidad));
  const maxHist = Math.max(1, ...data.historico.map((h) => h.costoEmpleador));
  const totalRegimen = data.distribucionRegimen.reduce((s, r) => s + r.cantidad, 0) || 1;

  return (
    <>
      <PageHeader title="Dashboard" description="Indicadores clave de personal y planilla." />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Empleados activos" value={data.empleadosActivos} hint={`${data.empleadosInactivos} inactivos`} icon={<Users className="h-5 w-5" />} />
        <StatCard label="Masa salarial mensual" value={formatCurrency(data.masaSalarialMensual)} hint="Suma de sueldos básicos" icon={<Banknote className="h-5 w-5" />} />
        <StatCard
          label={data.ultimoPeriodo ? `Neto ${formatPeriodo(data.ultimoPeriodo)}` : 'Última planilla'}
          value={formatCurrency(data.ultimoNeto)}
          hint={`Costo empleador: ${formatCurrency(data.ultimoCostoEmpleador)}`}
          icon={<Building2 className="h-5 w-5" />}
        />
        <StatCard
          label="Pendientes de atención"
          value={data.solicitudesPendientes}
          hint={`${data.contratosPorVencer} contrato(s) vencen en 30 días`}
          icon={data.solicitudesPendientes ? <Palmtree className="h-5 w-5" /> : <AlarmClock className="h-5 w-5" />}
        />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <Card className="p-5 lg:col-span-2">
          <h2 className="font-semibold">Costo de planilla por periodo</h2>
          <p className="mb-4 text-xs text-slate-500">Neto pagado vs. costo total del empleador (ingresos + EsSalud)</p>
          {data.historico.length === 0 ? (
            <p className="py-8 text-center text-sm text-slate-500">Aún no hay planillas generadas.</p>
          ) : (
            <div className="flex h-56 items-end gap-4" role="img" aria-label="Gráfico de barras del costo de planilla">
              {data.historico.map((h) => (
                <div key={h.periodo} className="flex flex-1 flex-col items-center gap-2">
                  <div className="flex h-44 w-full items-end justify-center gap-1">
                    <div className="w-1/3 rounded-t bg-brand-500" style={{ height: `${(h.neto / maxHist) * 100}%` }} title={`Neto: ${formatCurrency(h.neto)}`} />
                    <div className="w-1/3 rounded-t bg-slate-300 dark:bg-slate-600" style={{ height: `${(h.costoEmpleador / maxHist) * 100}%` }} title={`Costo: ${formatCurrency(h.costoEmpleador)}`} />
                  </div>
                  <span className="text-xs text-slate-500">{formatPeriodo(h.periodo)}</span>
                </div>
              ))}
            </div>
          )}
          <div className="mt-3 flex gap-4 text-xs text-slate-500">
            <span className="flex items-center gap-1"><span className="h-2 w-2 rounded-full bg-brand-500" /> Neto</span>
            <span className="flex items-center gap-1"><span className="h-2 w-2 rounded-full bg-slate-300 dark:bg-slate-600" /> Costo empleador</span>
          </div>
        </Card>

        <Card className="p-5">
          <h2 className="mb-4 font-semibold">Sistema pensionario</h2>
          <div className="space-y-3">
            {data.distribucionRegimen.map((r) => (
              <div key={r.etiqueta}>
                <div className="mb-1 flex justify-between text-sm">
                  <span>{r.etiqueta.toUpperCase()}</span>
                  <span className="tabular-nums text-slate-500">{r.cantidad} ({Math.round((r.cantidad / totalRegimen) * 100)}%)</span>
                </div>
                <div className="h-2 rounded-full bg-slate-100 dark:bg-slate-800">
                  <div className={`h-2 rounded-full ${r.etiqueta === 'Afp' ? 'bg-brand-500' : 'bg-emerald-500'}`} style={{ width: `${(r.cantidad / totalRegimen) * 100}%` }} />
                </div>
              </div>
            ))}
          </div>

          <h2 className="mb-4 mt-8 font-semibold">Empleados por área</h2>
          <ul className="space-y-2">
            {data.empleadosPorArea.map((a) => (
              <li key={a.etiqueta} className="text-sm">
                <div className="mb-1 flex justify-between"><span>{a.etiqueta}</span><span className="tabular-nums text-slate-500">{a.cantidad}</span></div>
                <div className="h-1.5 rounded-full bg-slate-100 dark:bg-slate-800">
                  <div className="h-1.5 rounded-full bg-brand-400" style={{ width: `${(a.cantidad / maxArea) * 100}%` }} />
                </div>
              </li>
            ))}
          </ul>
        </Card>
      </div>

      <div className="mt-6 flex flex-wrap gap-3 text-sm">
        <Link to="/planillas" className="text-brand-600 hover:underline">Generar planilla →</Link>
        <Link to="/vacaciones" className="text-brand-600 hover:underline">Revisar solicitudes de vacaciones →</Link>
        <Link to="/contratos" className="text-brand-600 hover:underline">Ver contratos →</Link>
      </div>
    </>
  );
}

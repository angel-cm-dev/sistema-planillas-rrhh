import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Printer } from 'lucide-react';
import { api } from '@/lib/api';
import { useAsync } from '@/lib/hooks';
import { formatCurrency, formatNumber, formatPeriodo, REGIMEN_LABEL } from '@/lib/format';
import type { Boleta } from '@/lib/types';
import { Button, ErrorState, Spinner } from '@/components/ui';

/** Fila concepto → monto. Los montos en cero se omiten para que la boleta sea legible. */
function Linea({ concepto, monto, detalle, mostrarCero }: { concepto: string; monto: number; detalle?: string; mostrarCero?: boolean }) {
  if (!monto && !mostrarCero) return null;
  return (
    <div className="flex justify-between gap-4 py-1 text-sm">
      <span>{concepto}{detalle && <span className="text-xs text-slate-500"> ({detalle})</span>}</span>
      <span className="tabular-nums">{formatCurrency(monto)}</span>
    </div>
  );
}

export default function BoletaPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { data: b, error, loading, reload } = useAsync(() => api.get<Boleta>(`/api/boletas/${id}`), [id]);

  if (loading && !b) return <Spinner />;
  if (error) return <ErrorState message={error.message} onRetry={error.status >= 500 ? reload : undefined} />;
  if (!b) return null;

  return (
    <>
      <div className="mb-4 flex items-center justify-between print:hidden">
        <button onClick={() => navigate(-1)} className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-brand-600"><ArrowLeft className="h-4 w-4" /> Volver</button>
        <Button onClick={() => window.print()}><Printer className="h-4 w-4" /> Imprimir / PDF</Button>
      </div>

      <article className="mx-auto max-w-3xl rounded-xl border border-slate-200 bg-white p-8 text-slate-900 shadow-sm print:max-w-none print:border-0 print:p-0 print:shadow-none">
        <header className="flex flex-col justify-between gap-4 border-b border-slate-300 pb-4 sm:flex-row">
          <div>
            <h1 className="text-lg font-bold">EMPRESA DEMO S.A.C.</h1>
            <p className="text-xs text-slate-600">RUC 20000000001 (ficticio) · Av. Ejemplo 123, Lima</p>
          </div>
          <div className="sm:text-right">
            <p className="text-base font-semibold">BOLETA DE PAGO</p>
            <p className="text-sm">Periodo: {formatPeriodo(b.periodo)}</p>
            {b.estadoPlanilla !== 'Cerrada' && <p className="text-xs font-semibold text-amber-700">BORRADOR — no válido como constancia</p>}
          </div>
        </header>

        <section className="grid gap-x-8 gap-y-1 border-b border-slate-300 py-4 text-sm sm:grid-cols-2">
          <p><span className="text-slate-500">Trabajador:</span> {b.empleadoNombre}</p>
          <p><span className="text-slate-500">DNI:</span> {b.empleadoDni}</p>
          <p><span className="text-slate-500">Cargo:</span> {b.cargo}</p>
          <p><span className="text-slate-500">Área:</span> {b.area}</p>
          <p><span className="text-slate-500">Sistema pensionario:</span> {REGIMEN_LABEL[b.regimen]}{b.afpNombre ? ` — ${b.afpNombre}` : ''}</p>
          <p><span className="text-slate-500">Días de falta:</span> {b.diasFalta}</p>
        </section>

        <section className="grid gap-6 py-4 sm:grid-cols-3">
          <div>
            <h2 className="mb-2 border-b border-slate-200 pb-1 text-xs font-semibold uppercase tracking-wide text-slate-600">Ingresos</h2>
            <Linea concepto="Sueldo básico" monto={b.sueldoBasico} mostrarCero />
            <Linea concepto="Asignación familiar" monto={b.asignacionFamiliar} />
            <Linea concepto="Horas extra" monto={b.montoHorasExtra} detalle={`${formatNumber(b.horasExtra25)} h al 25%, ${formatNumber(b.horasExtra35)} h al 35%`} />
            <Linea concepto="(-) Faltas" monto={-b.descuentoFaltas} />
            <div className="mt-2 flex justify-between border-t border-slate-200 pt-2 text-sm font-semibold">
              <span>Total ingresos</span><span className="tabular-nums">{formatCurrency(b.totalIngresos)}</span>
            </div>
          </div>
          <div>
            <h2 className="mb-2 border-b border-slate-200 pb-1 text-xs font-semibold uppercase tracking-wide text-slate-600">Descuentos</h2>
            <Linea concepto="ONP" monto={b.onp} />
            <Linea concepto="AFP aporte" monto={b.afpAporte} />
            <Linea concepto="AFP prima seguro" monto={b.afpPrima} />
            <Linea concepto="AFP comisión" monto={b.afpComision} />
            <Linea concepto="Renta 5ta categoría" monto={b.renta5ta} />
            <div className="mt-2 flex justify-between border-t border-slate-200 pt-2 text-sm font-semibold">
              <span>Total descuentos</span><span className="tabular-nums">{formatCurrency(b.totalDescuentos)}</span>
            </div>
          </div>
          <div>
            <h2 className="mb-2 border-b border-slate-200 pb-1 text-xs font-semibold uppercase tracking-wide text-slate-600">Aportes del empleador</h2>
            <Linea concepto="EsSalud" monto={b.esSalud} mostrarCero />
            <p className="mt-2 text-xs text-slate-500">No se descuentan al trabajador.</p>
          </div>
        </section>

        <div className="flex items-center justify-between rounded-lg bg-slate-100 px-4 py-3">
          <span className="font-semibold">NETO A PAGAR</span>
          <span className="text-xl font-bold tabular-nums">{formatCurrency(b.netoPagar)}</span>
        </div>

        <div className="mt-16 grid grid-cols-2 gap-12 text-center text-xs text-slate-600">
          <div className="border-t border-slate-400 pt-1">Empleador</div>
          <div className="border-t border-slate-400 pt-1">Trabajador</div>
        </div>

        <p className="mt-8 text-[11px] leading-snug text-slate-500">
          Documento generado por un sistema de demostración. Tasas y parámetros referenciales y configurables;
          la renta de 5ta categoría es una aproximación didáctica. No válido para fines legales o tributarios.
        </p>
      </article>

      <p className="mt-4 text-center text-sm print:hidden">
        <Link to="/mis-boletas" className="text-brand-600 hover:underline">Ver todas mis boletas</Link>
      </p>
    </>
  );
}

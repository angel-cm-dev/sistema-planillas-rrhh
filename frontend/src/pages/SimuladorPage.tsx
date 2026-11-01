import { useState, type FormEvent } from 'react';
import { Calculator } from 'lucide-react';
import { api, ApiError } from '@/lib/api';
import { useAsync } from '@/lib/hooks';
import { formatCurrency, formatNumber, parseHorasExtra } from '@/lib/format';
import type { Afp, Regimen, ResultadoCalculo } from '@/lib/types';
import { Alert, Button, Card, Field, Input, PageHeader, Select } from '@/components/ui';

export default function SimuladorPage() {
  const afps = useAsync(() => api.get<Afp[]>('/api/catalogos/afps'), []);
  const [sueldo, setSueldo] = useState('3500');
  const [hijos, setHijos] = useState(false);
  const [regimen, setRegimen] = useState<Regimen>('Afp');
  const [afpId, setAfpId] = useState('1');
  const [faltas, setFaltas] = useState('0');
  const [horas, setHoras] = useState('');
  const [resultado, setResultado] = useState<ResultadoCalculo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [calculando, setCalculando] = useState(false);

  const calcular = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setCalculando(true);
    try {
      setResultado(await api.post<ResultadoCalculo>('/api/planillas/simular', {
        sueldoBasico: Number(sueldo),
        tieneHijos: hijos,
        regimen,
        afpId: regimen === 'Afp' ? Number(afpId) : null,
        diasFalta: Number(faltas) || 0,
        horasExtraPorDia: parseHorasExtra(horas),
      }));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Error al calcular');
    } finally {
      setCalculando(false);
    }
  };

  const r = resultado;
  return (
    <>
      <PageHeader title="Simulador de boleta" description="Calcula una boleta hipotética con los parámetros vigentes, sin guardar nada. Ideal para entender cada concepto." />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="p-5">
          <form onSubmit={calcular} className="space-y-4">
            <Field label="Sueldo básico (S/)" htmlFor="sueldo"><Input id="sueldo" type="number" min={1} step="0.01" value={sueldo} onChange={(e) => setSueldo(e.target.value)} /></Field>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Sistema pensionario" htmlFor="regimen">
                <Select id="regimen" value={regimen} onChange={(e) => setRegimen(e.target.value as Regimen)}>
                  <option value="Onp">ONP</option>
                  <option value="Afp">AFP</option>
                </Select>
              </Field>
              {regimen === 'Afp' && (
                <Field label="AFP" htmlFor="afp">
                  <Select id="afp" value={afpId} onChange={(e) => setAfpId(e.target.value)}>
                    {afps.data?.map((a) => <option key={a.id} value={a.id}>{a.nombre} (comisión {a.comisionPct}%)</option>)}
                  </Select>
                </Field>
              )}
              <Field label="Días de falta" htmlFor="faltas"><Input id="faltas" type="number" min={0} max={30} value={faltas} onChange={(e) => setFaltas(e.target.value)} /></Field>
              <Field label="Horas extra por día" htmlFor="horas" hint="Ej.: 2, 3, 1 (una cifra por día)"><Input id="horas" value={horas} onChange={(e) => setHoras(e.target.value)} placeholder="2, 3" /></Field>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={hijos} onChange={(e) => setHijos(e.target.checked)} className="h-4 w-4 rounded" /> Tiene hijos (asignación familiar)
            </label>
            {error && <Alert>{error}</Alert>}
            <Button type="submit" loading={calculando}><Calculator className="h-4 w-4" /> Calcular</Button>
          </form>
        </Card>

        <Card className="p-5">
          {!r ? (
            <p className="py-16 text-center text-sm text-slate-500">Complete los datos y presione “Calcular”.</p>
          ) : (
            <div className="space-y-4 text-sm">
              <Bloque titulo="Ingresos" filas={[
                ['Sueldo básico', r.sueldoBasico],
                ['Asignación familiar (10% RMV)', r.asignacionFamiliar],
                [`Horas extra (${formatNumber(r.horasExtra25)} h 25% + ${formatNumber(r.horasExtra35)} h 35%, valor hora ${formatCurrency(r.valorHora)})`, r.montoHorasExtra],
                ['(-) Descuento por faltas', -r.descuentoFaltas],
              ]} total={['Remuneración computable', r.totalIngresos]} />
              <Bloque titulo="Descuentos al trabajador" filas={[
                ['ONP', r.onp], ['AFP aporte', r.afpAporte], ['AFP prima de seguro', r.afpPrima], ['AFP comisión', r.afpComision],
                ['Renta 5ta (aprox.)', r.renta5ta],
              ]} total={['Total descuentos', r.totalDescuentos]} />
              <div className="flex justify-between rounded-lg bg-brand-50 px-4 py-3 text-base font-semibold text-brand-900 dark:bg-brand-900/40 dark:text-brand-100">
                <span>Neto a pagar</span><span className="tabular-nums">{formatCurrency(r.netoPagar)}</span>
              </div>
              <Bloque titulo="Costo para el empleador" filas={[['EsSalud (9%)', r.esSalud]]} total={['Costo total', r.costoEmpleador]} />
            </div>
          )}
        </Card>
      </div>
    </>
  );
}

function Bloque({ titulo, filas, total }: { titulo: string; filas: [string, number][]; total: [string, number] }) {
  return (
    <div>
      <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">{titulo}</h3>
      {filas.filter(([, v]) => v !== 0).map(([k, v]) => (
        <div key={k} className="flex justify-between gap-4 py-0.5"><span>{k}</span><span className="tabular-nums">{formatCurrency(v)}</span></div>
      ))}
      <div className="mt-1 flex justify-between border-t border-slate-200 pt-1 font-semibold dark:border-slate-700">
        <span>{total[0]}</span><span className="tabular-nums">{formatCurrency(total[1])}</span>
      </div>
    </div>
  );
}

import { useEffect, useState, type FormEvent } from 'react';
import { api, ApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { useAsync } from '@/lib/hooks';
import type { Afp, Parametros } from '@/lib/types';
import { Alert, Button, Card, ErrorState, Field, Input, PageHeader, Spinner } from '@/components/ui';

const CAMPOS: { key: keyof Parametros; label: string; hint?: string }[] = [
  { key: 'uit', label: 'UIT (S/)', hint: 'Unidad Impositiva Tributaria del año' },
  { key: 'rmv', label: 'RMV (S/)', hint: 'Remuneración Mínima Vital' },
  { key: 'asignacionFamiliarPct', label: 'Asignación familiar (% RMV)' },
  { key: 'onpPct', label: 'ONP (%)' },
  { key: 'esSaludPct', label: 'EsSalud (%)', hint: 'Aporte del empleador' },
  { key: 'horaExtra25Pct', label: 'Sobretasa 2 primeras horas (%)' },
  { key: 'horaExtra35Pct', label: 'Sobretasa horas siguientes (%)' },
  { key: 'topePrimaAfp', label: 'Rem. máxima asegurable (S/)', hint: 'Tope para la prima de seguro AFP' },
  { key: 'horasJornadaDiaria', label: 'Horas de jornada diaria' },
];

export default function ParametrosPage() {
  const { hasRole } = useAuth();
  const editable = hasRole('Admin');
  const params = useAsync(() => api.get<Parametros>('/api/catalogos/parametros'), []);
  const afps = useAsync(() => api.get<Afp[]>('/api/catalogos/afps'), []);
  const [form, setForm] = useState<Parametros | null>(null);
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => { if (params.data) setForm(params.data); }, [params.data]);

  const guardar = async (e: FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setSaving(true);
    setMsg(null);
    try {
      setForm(await api.put<Parametros>('/api/catalogos/parametros', form));
      setMsg({ tone: 'success', text: 'Parámetros actualizados. Se aplicarán en la próxima generación de planilla.' });
    } catch (err) {
      setMsg({ tone: 'error', text: err instanceof ApiError ? err.message : 'Error al guardar' });
    } finally {
      setSaving(false);
    }
  };

  const guardarAfp = async (afp: Afp) => {
    try {
      await api.put(`/api/catalogos/afps/${afp.id}`, afp);
      setMsg({ tone: 'success', text: `Tasas de ${afp.nombre} actualizadas.` });
      afps.reload();
    } catch (err) {
      setMsg({ tone: 'error', text: err instanceof ApiError ? err.message : 'Error al guardar' });
    }
  };

  if (params.loading && !form) return <Spinner />;
  if (params.error) return <ErrorState message={params.error.message} onRetry={params.reload} />;

  return (
    <>
      <PageHeader title="Parámetros de planilla" description="Valores legales usados por el cálculo. Son REFERENCIALES: actualícelos según la normativa vigente." />
      {!editable && <div className="mb-4"><Alert tone="info">Solo el administrador puede modificar estos valores.</Alert></div>}
      {msg && <div className="mb-4"><Alert tone={msg.tone}>{msg.text}</Alert></div>}

      {form && (
        <Card className="mb-6 p-5">
          <form onSubmit={guardar}>
            <fieldset disabled={!editable} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {CAMPOS.map((c) => (
                <Field key={c.key} label={c.label} htmlFor={c.key} hint={c.hint}>
                  <Input id={c.key} type="number" step="0.01" value={form[c.key]}
                    onChange={(e) => setForm({ ...form, [c.key]: Number(e.target.value) })} />
                </Field>
              ))}
            </fieldset>
            {editable && <div className="mt-4"><Button type="submit" loading={saving}>Guardar parámetros</Button></div>}
          </form>
        </Card>
      )}

      <Card className="p-5">
        <h2 className="mb-4 font-semibold">Tasas de AFP (%)</h2>
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500">
                <th className="py-2 pr-4">AFP</th><th className="py-2 pr-4">Aporte</th><th className="py-2 pr-4">Prima seguro</th><th className="py-2 pr-4">Comisión flujo</th><th />
              </tr>
            </thead>
            <tbody>
              {afps.data?.map((a) => <AfpRow key={a.id} afp={a} editable={editable} onSave={guardarAfp} />)}
            </tbody>
          </table>
        </div>
      </Card>
    </>
  );
}

function AfpRow({ afp, editable, onSave }: { afp: Afp; editable: boolean; onSave: (a: Afp) => void }) {
  const [v, setV] = useState(afp);
  const campo = (k: 'aportePct' | 'primaSeguroPct' | 'comisionPct') => (
    <td className="py-2 pr-4">
      <Input type="number" step="0.01" className="w-24" disabled={!editable} value={v[k]} aria-label={`${afp.nombre} ${k}`}
        onChange={(e) => setV({ ...v, [k]: Number(e.target.value) })} />
    </td>
  );
  return (
    <tr className="border-t border-slate-100 dark:border-slate-800">
      <td className="py-2 pr-4 font-medium">{afp.nombre}</td>
      {campo('aportePct')}{campo('primaSeguroPct')}{campo('comisionPct')}
      <td className="py-2">{editable && <Button variant="secondary" onClick={() => onSave(v)}>Guardar</Button>}</td>
    </tr>
  );
}

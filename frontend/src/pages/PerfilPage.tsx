import { api } from '@/lib/api';
import { useAsync } from '@/lib/hooks';
import { formatCurrency, formatDate, REGIMEN_LABEL } from '@/lib/format';
import type { EmpleadoDetail } from '@/lib/types';
import { Card, ErrorState, PageHeader, Spinner } from '@/components/ui';

export default function PerfilPage() {
  const { data: e, error, loading, reload } = useAsync(() => api.get<EmpleadoDetail>('/api/perfil'), []);
  if (loading && !e) return <Spinner />;
  if (error) return <ErrorState message={error.message} onRetry={reload} />;
  if (!e) return null;

  const filas: [string, string][] = [
    ['DNI', e.dni],
    ['Correo', e.email],
    ['Área', e.area],
    ['Cargo', e.cargo],
    ['Fecha de ingreso', formatDate(e.fechaIngreso)],
    ['Sueldo básico', formatCurrency(e.sueldoBasico)],
    ['Sistema pensionario', `${REGIMEN_LABEL[e.regimen]}${e.afp ? ` — ${e.afp}` : ''}`],
    ['Asignación familiar', e.tieneHijos ? 'Sí' : 'No'],
  ];

  return (
    <>
      <PageHeader title={`${e.nombres} ${e.apellidos}`} description="Datos de su ficha de empleado (solo lectura)." />
      <Card className="max-w-2xl">
        <dl className="divide-y divide-slate-100 dark:divide-slate-800">
          {filas.map(([k, v]) => (
            <div key={k} className="grid grid-cols-3 gap-4 px-5 py-3 text-sm">
              <dt className="text-slate-500">{k}</dt>
              <dd className="col-span-2 font-medium">{v}</dd>
            </div>
          ))}
        </dl>
      </Card>
    </>
  );
}

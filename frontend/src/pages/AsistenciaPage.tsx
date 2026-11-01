import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Search } from 'lucide-react';
import { api, ApiError, toQuery } from '@/lib/api';
import { esGestor, useAuth } from '@/lib/auth';
import { useAsync, useDebounce } from '@/lib/hooks';
import { formatDate, formatNumber, todayIso } from '@/lib/format';
import { asistenciaSchema, type AsistenciaForm } from '@/lib/schemas';
import type { Asistencia, EmpleadoListItem, PagedResult } from '@/lib/types';
import { DataTable, Pagination, type Column } from '@/components/DataTable';
import { Alert, Badge, Button, Card, estadoTone, Field, Input, Modal, PageHeader, Select } from '@/components/ui';

const ESTADOS = ['Presente', 'Tardanza', 'Falta', 'Licencia', 'Vacaciones'] as const;

export default function AsistenciaPage() {
  const { user } = useAuth();
  const gestor = esGestor(user?.rol);
  const [search, setSearch] = useState('');
  const [desde, setDesde] = useState(todayIso(-45));
  const [hasta, setHasta] = useState(todayIso());
  const [estado, setEstado] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const debounced = useDebounce(search);

  const { data, error, loading, reload } = useAsync(
    () => api.get<PagedResult<Asistencia>>(`/api/asistencias${toQuery({ page, pageSize: 15, search: debounced, desde, hasta, estado })}`),
    [page, debounced, desde, hasta, estado],
  );
  useEffect(() => setPage(1), [debounced, desde, hasta, estado]);

  const columns: Column<Asistencia>[] = [
    { key: 'fecha', header: 'Fecha', render: (a) => formatDate(a.fecha) },
    ...(gestor ? [{ key: 'empleado', header: 'Empleado', render: (a: Asistencia) => <span className="font-medium">{a.empleado}</span> }] : []),
    { key: 'estado', header: 'Estado', render: (a) => <Badge tone={estadoTone(a.estado)}>{a.estado}</Badge> },
    { key: 'entrada', header: 'Entrada', render: (a) => a.horaEntrada?.slice(0, 5) ?? '—' },
    { key: 'salida', header: 'Salida', render: (a) => a.horaSalida?.slice(0, 5) ?? '—' },
    { key: 'he', header: 'Horas extra', className: 'text-right', render: (a) => (a.horasExtra ? formatNumber(a.horasExtra) : '—') },
    { key: 'obs', header: 'Observación', render: (a) => <span className="text-slate-500">{a.observacion ?? ''}</span> },
  ];

  return (
    <>
      <PageHeader
        title={gestor ? 'Asistencia' : 'Mi asistencia'}
        description="Las faltas y horas extra del mes alimentan el cálculo de planilla."
        actions={gestor && <Button onClick={() => setCreating(true)}><Plus className="h-4 w-4" /> Registrar asistencia</Button>}
      />
      <Card>
        <div className="grid gap-3 border-b border-slate-200 p-4 dark:border-slate-800 sm:grid-cols-2 lg:grid-cols-4">
          {gestor && (
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input className="pl-9" placeholder="Buscar empleado" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Buscar" />
            </div>
          )}
          <Input type="date" value={desde} onChange={(e) => setDesde(e.target.value)} aria-label="Desde" />
          <Input type="date" value={hasta} onChange={(e) => setHasta(e.target.value)} aria-label="Hasta" />
          <Select value={estado} onChange={(e) => setEstado(e.target.value)} aria-label="Estado">
            <option value="">Todos los estados</option>
            {ESTADOS.map((e) => <option key={e} value={e}>{e}</option>)}
          </Select>
        </div>
        <DataTable columns={columns} rows={data?.items} loading={loading} error={error} onRetry={reload} rowKey={(a) => a.id} />
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
      {creating && <AsistenciaModal onClose={() => setCreating(false)} onSaved={() => { setCreating(false); reload(); }} />}
    </>
  );
}

function AsistenciaModal({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const empleados = useAsync(() => api.get<PagedResult<EmpleadoListItem>>('/api/empleados?pageSize=100&activo=true'), []);
  const [serverError, setServerError] = useState<string | null>(null);
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<AsistenciaForm>({
    resolver: zodResolver(asistenciaSchema),
    defaultValues: { empleadoId: 0, fecha: todayIso(), estado: 'Presente', horaEntrada: '08:00', horaSalida: '17:00', horasExtra: 0, observacion: '' },
  });
  const estado = watch('estado');
  const sinHorario = estado === 'Falta' || estado === 'Licencia' || estado === 'Vacaciones';

  const onSubmit = async (v: AsistenciaForm) => {
    setServerError(null);
    try {
      await api.post('/api/asistencias', {
        ...v,
        // El backend (TimeOnly) espera "HH:mm:ss"; el input type="time" entrega "HH:mm".
        horaEntrada: sinHorario || !v.horaEntrada ? null : `${v.horaEntrada.slice(0, 5)}:00`,
        horaSalida: sinHorario || !v.horaSalida ? null : `${v.horaSalida.slice(0, 5)}:00`,
        observacion: v.observacion || null,
      });
      onSaved();
    } catch (e) {
      setServerError(e instanceof ApiError ? e.message : 'Error al guardar');
    }
  };

  return (
    <Modal open title="Registrar asistencia" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert>{serverError}</Alert>}
        <Alert tone="info">Si ya existe un registro para ese empleado y fecha, se actualizará.</Alert>
        <Field label="Empleado" htmlFor="empleadoId" error={errors.empleadoId?.message}>
          <Select id="empleadoId" {...register('empleadoId')}>
            <option value={0}>Seleccione…</option>
            {empleados.data?.items.map((e) => <option key={e.id} value={e.id}>{e.nombreCompleto}</option>)}
          </Select>
        </Field>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Fecha" htmlFor="fecha" error={errors.fecha?.message}><Input id="fecha" type="date" max={todayIso()} {...register('fecha')} /></Field>
          <Field label="Estado" htmlFor="estado">
            <Select id="estado" {...register('estado')}>{ESTADOS.map((e) => <option key={e} value={e}>{e}</option>)}</Select>
          </Field>
          {!sinHorario && (
            <>
              <Field label="Hora de entrada" htmlFor="horaEntrada"><Input id="horaEntrada" type="time" {...register('horaEntrada')} /></Field>
              <Field label="Hora de salida" htmlFor="horaSalida"><Input id="horaSalida" type="time" {...register('horaSalida')} /></Field>
            </>
          )}
        </div>
        <Field label="Horas extra" htmlFor="horasExtra" error={errors.horasExtra?.message} hint="Las 2 primeras se pagan con 25% de sobretasa y las siguientes con 35%.">
          <Input id="horasExtra" type="number" step="0.5" min={0} max={12} {...register('horasExtra')} />
        </Field>
        <Field label="Observación" htmlFor="observacion"><Input id="observacion" {...register('observacion')} /></Field>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={isSubmitting}>Guardar</Button>
        </div>
      </form>
    </Modal>
  );
}

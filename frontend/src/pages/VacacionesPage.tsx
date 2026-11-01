import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Check, Plus, Search, X } from 'lucide-react';
import { api, ApiError, toQuery } from '@/lib/api';
import { esGestor, useAuth } from '@/lib/auth';
import { useAsync, useDebounce } from '@/lib/hooks';
import { formatDate, todayIso } from '@/lib/format';
import { solicitudSchema, type SolicitudForm } from '@/lib/schemas';
import type { EmpleadoListItem, PagedResult, SaldoVacaciones, SolicitudVacaciones } from '@/lib/types';
import { DataTable, Pagination, type Column } from '@/components/DataTable';
import { Alert, Badge, Button, Card, estadoTone, Field, Input, Modal, PageHeader, Select, StatCard } from '@/components/ui';

export default function VacacionesPage() {
  const { user } = useAuth();
  const gestor = esGestor(user?.rol);
  const [estado, setEstado] = useState(gestor ? 'Pendiente' : '');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [rechazando, setRechazando] = useState<SolicitudVacaciones | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const debounced = useDebounce(search);

  const { data, error, loading, reload } = useAsync(
    () => api.get<PagedResult<SolicitudVacaciones>>(`/api/vacaciones${toQuery({ page, pageSize: 10, estado, search: debounced })}`),
    [page, estado, debounced],
  );
  // El saldo propio solo aplica si el usuario está vinculado a un empleado.
  const saldo = useAsync(
    () => (user?.empleadoId ? api.get<SaldoVacaciones>('/api/vacaciones/saldo') : Promise.resolve(null)),
    [user?.empleadoId],
  );
  useEffect(() => setPage(1), [estado, debounced]);

  const ejecutar = async (fn: () => Promise<unknown>) => {
    setActionError(null);
    try {
      await fn();
      reload();
      saldo.reload();
    } catch (e) {
      setActionError(e instanceof ApiError ? e.message : 'No se pudo completar la acción');
    }
  };

  const columns: Column<SolicitudVacaciones>[] = [
    ...(gestor ? [{ key: 'empleado', header: 'Empleado', render: (s: SolicitudVacaciones) => <span className="font-medium">{s.empleado}</span> }] : []),
    { key: 'periodo', header: 'Periodo', render: (s) => `${formatDate(s.fechaInicio)} → ${formatDate(s.fechaFin)}` },
    { key: 'dias', header: 'Días', className: 'text-right', render: (s) => s.dias },
    { key: 'motivo', header: 'Motivo', render: (s) => <span className="text-slate-500">{s.motivo ?? '—'}</span> },
    {
      key: 'estado', header: 'Estado',
      render: (s) => (
        <div>
          <Badge tone={estadoTone(s.estado)}>{s.estado}</Badge>
          {s.comentarioRevision && <p className="mt-1 max-w-xs truncate text-xs text-slate-500" title={s.comentarioRevision}>{s.comentarioRevision}</p>}
        </div>
      ),
    },
    {
      key: 'acciones', header: '', className: 'text-right',
      render: (s) => s.estado === 'Pendiente' && (
        <div className="flex justify-end gap-1">
          {gestor && (
            <>
              <Button variant="secondary" onClick={() => ejecutar(() => api.post(`/api/vacaciones/${s.id}/aprobar`, { comentario: 'Aprobado' }))} aria-label="Aprobar">
                <Check className="h-4 w-4 text-emerald-600" /> Aprobar
              </Button>
              <Button variant="secondary" onClick={() => setRechazando(s)} aria-label="Rechazar"><X className="h-4 w-4 text-rose-600" /> Rechazar</Button>
            </>
          )}
          {(!gestor || s.empleadoId === user?.empleadoId) && (
            <Button variant="ghost" onClick={() => ejecutar(() => api.post(`/api/vacaciones/${s.id}/cancelar`))}>Cancelar</Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Vacaciones"
        description="Flujo: el empleado solicita → RR.HH. aprueba o rechaza. 30 días calendario por año de servicio."
        actions={<Button onClick={() => setCreating(true)}><Plus className="h-4 w-4" /> Nueva solicitud</Button>}
      />
      {saldo.data && (
        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <StatCard label="Días ganados" value={saldo.data.diasGanados} hint="30 por cada año completo" />
          <StatCard label="Días comprometidos" value={saldo.data.diasConsumidos} hint="Pendientes + aprobados" />
          <StatCard label="Días disponibles" value={saldo.data.diasDisponibles} />
        </div>
      )}
      {actionError && <div className="mb-4"><Alert>{actionError}</Alert></div>}
      <Card>
        <div className="grid gap-3 border-b border-slate-200 p-4 dark:border-slate-800 sm:grid-cols-3">
          {gestor && (
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <Input className="pl-9" placeholder="Buscar empleado" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Buscar" />
            </div>
          )}
          <Select value={estado} onChange={(e) => setEstado(e.target.value)} aria-label="Estado">
            <option value="">Todos los estados</option>
            {['Pendiente', 'Aprobada', 'Rechazada', 'Cancelada'].map((e) => <option key={e} value={e}>{e}</option>)}
          </Select>
        </div>
        <DataTable columns={columns} rows={data?.items} loading={loading} error={error} onRetry={reload} rowKey={(s) => s.id}
          emptyMessage={estado === 'Pendiente' ? 'No hay solicitudes pendientes de revisión.' : undefined} />
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>

      {creating && <SolicitudModal gestor={gestor} onClose={() => setCreating(false)} onSaved={() => { setCreating(false); reload(); saldo.reload(); }} />}
      {rechazando && (
        <RechazoModal
          solicitud={rechazando}
          onClose={() => setRechazando(null)}
          onConfirm={(comentario) => ejecutar(() => api.post(`/api/vacaciones/${rechazando.id}/rechazar`, { comentario })).then(() => setRechazando(null))}
        />
      )}
    </>
  );
}

function SolicitudModal({ gestor, onClose, onSaved }: { gestor: boolean; onClose: () => void; onSaved: () => void }) {
  const empleados = useAsync(
    () => (gestor ? api.get<PagedResult<EmpleadoListItem>>('/api/empleados?pageSize=100&activo=true') : Promise.resolve(null)),
    [gestor],
  );
  const [serverError, setServerError] = useState<string | null>(null);
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<SolicitudForm>({
    resolver: zodResolver(solicitudSchema),
    defaultValues: { fechaInicio: todayIso(7), fechaFin: todayIso(13), motivo: '' },
  });
  const [ini, fin] = watch(['fechaInicio', 'fechaFin']);
  const dias = ini && fin && fin >= ini ? Math.round((Date.parse(fin) - Date.parse(ini)) / 86_400_000) + 1 : 0;

  const onSubmit = async (v: SolicitudForm) => {
    setServerError(null);
    try {
      await api.post('/api/vacaciones', { ...v, empleadoId: v.empleadoId || null, motivo: v.motivo || null });
      onSaved();
    } catch (e) {
      setServerError(e instanceof ApiError ? e.message : 'Error al registrar');
    }
  };

  return (
    <Modal open title="Solicitar vacaciones" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert>{serverError}</Alert>}
        {gestor && (
          <Field label="Empleado" htmlFor="empleadoId" hint="Déjelo vacío para registrar una solicitud propia.">
            <Select id="empleadoId" {...register('empleadoId')}>
              <option value="">(Yo mismo)</option>
              {empleados.data?.items.map((e) => <option key={e.id} value={e.id}>{e.nombreCompleto}</option>)}
            </Select>
          </Field>
        )}
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Desde" htmlFor="fechaInicio" error={errors.fechaInicio?.message}><Input id="fechaInicio" type="date" min={todayIso()} {...register('fechaInicio')} /></Field>
          <Field label="Hasta" htmlFor="fechaFin" error={errors.fechaFin?.message}><Input id="fechaFin" type="date" min={ini} {...register('fechaFin')} /></Field>
        </div>
        <p className="text-sm text-slate-500">Total: <strong>{dias}</strong> día(s) calendario.</p>
        <Field label="Motivo (opcional)" htmlFor="motivo"><Input id="motivo" {...register('motivo')} /></Field>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={isSubmitting}>Enviar solicitud</Button>
        </div>
      </form>
    </Modal>
  );
}

function RechazoModal({ solicitud, onClose, onConfirm }: { solicitud: SolicitudVacaciones; onClose: () => void; onConfirm: (c: string) => void }) {
  const [comentario, setComentario] = useState('');
  const [error, setError] = useState<string | undefined>();
  return (
    <Modal open title={`Rechazar solicitud de ${solicitud.empleado}`} onClose={onClose}>
      <div className="space-y-4">
        <Field label="Motivo del rechazo" htmlFor="comentario" error={error}>
          <Input id="comentario" value={comentario} onChange={(e) => setComentario(e.target.value)} />
        </Field>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button variant="danger" onClick={() => (comentario.trim() ? onConfirm(comentario.trim()) : setError('Indique el motivo'))}>Rechazar</Button>
        </div>
      </div>
    </Modal>
  );
}

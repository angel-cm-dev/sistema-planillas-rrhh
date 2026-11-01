import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Search } from 'lucide-react';
import { api, ApiError, toQuery } from '@/lib/api';
import { useAsync, useDebounce } from '@/lib/hooks';
import { formatCurrency, formatDate, TIPO_CONTRATO_LABEL, todayIso } from '@/lib/format';
import { contratoSchema, type ContratoForm } from '@/lib/schemas';
import type { Contrato, EmpleadoListItem, PagedResult } from '@/lib/types';
import { DataTable, Pagination, type Column } from '@/components/DataTable';
import { Alert, Badge, Button, Card, estadoTone, Field, Input, Modal, PageHeader, Select } from '@/components/ui';

export default function ContratosPage() {
  const [search, setSearch] = useState('');
  const [estado, setEstado] = useState('Vigente');
  const [tipo, setTipo] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const debounced = useDebounce(search);

  const { data, error, loading, reload } = useAsync(
    () => api.get<PagedResult<Contrato>>(`/api/contratos${toQuery({ page, pageSize: 10, search: debounced, estado, tipo })}`),
    [page, debounced, estado, tipo],
  );
  useEffect(() => setPage(1), [debounced, estado, tipo]);

  const finalizar = async (c: Contrato) => {
    const fecha = prompt(`Fecha de cese para ${c.empleado} (AAAA-MM-DD):`, todayIso());
    if (!fecha) return;
    try {
      await api.post(`/api/contratos/${c.id}/finalizar`, { fechaCese: fecha });
      reload();
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'No se pudo finalizar el contrato');
    }
  };

  const hoy = todayIso();
  const en30 = todayIso(30);
  const columns: Column<Contrato>[] = [
    { key: 'empleado', header: 'Empleado', render: (c) => <span className="font-medium">{c.empleado}</span> },
    { key: 'tipo', header: 'Tipo', render: (c) => TIPO_CONTRATO_LABEL[c.tipo] },
    { key: 'inicio', header: 'Inicio', render: (c) => formatDate(c.fechaInicio) },
    {
      key: 'fin', header: 'Fin',
      render: (c) => (
        <span className="flex items-center gap-2">
          {formatDate(c.fechaFin)}
          {c.estado === 'Vigente' && c.fechaFin && c.fechaFin >= hoy && c.fechaFin <= en30 && <Badge tone="amber">Por vencer</Badge>}
        </span>
      ),
    },
    { key: 'sueldo', header: 'Sueldo', className: 'text-right', render: (c) => <span className="tabular-nums">{formatCurrency(c.sueldo)}</span> },
    { key: 'estado', header: 'Estado', render: (c) => <Badge tone={estadoTone(c.estado)}>{c.estado}</Badge> },
    {
      key: 'acciones', header: '', className: 'text-right',
      render: (c) => c.estado === 'Vigente' && <Button variant="secondary" onClick={() => finalizar(c)}>Finalizar</Button>,
    },
  ];

  return (
    <>
      <PageHeader
        title="Contratos"
        description="Un empleado tiene como máximo un contrato vigente; su sueldo se toma como sueldo básico."
        actions={<Button onClick={() => setCreating(true)}><Plus className="h-4 w-4" /> Nuevo contrato</Button>}
      />
      <Card>
        <div className="grid gap-3 border-b border-slate-200 p-4 dark:border-slate-800 sm:grid-cols-3">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <Input className="pl-9" placeholder="Buscar empleado" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Buscar" />
          </div>
          <Select value={estado} onChange={(e) => setEstado(e.target.value)} aria-label="Estado">
            <option value="">Todos los estados</option>
            <option value="Vigente">Vigentes</option>
            <option value="Finalizado">Finalizados</option>
          </Select>
          <Select value={tipo} onChange={(e) => setTipo(e.target.value)} aria-label="Tipo">
            <option value="">Todos los tipos</option>
            {Object.entries(TIPO_CONTRATO_LABEL).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </Select>
        </div>
        <DataTable columns={columns} rows={data?.items} loading={loading} error={error} onRetry={reload} rowKey={(c) => c.id} />
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>
      {creating && <ContratoModal onClose={() => setCreating(false)} onSaved={() => { setCreating(false); reload(); }} />}
    </>
  );
}

function ContratoModal({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const empleados = useAsync(() => api.get<PagedResult<EmpleadoListItem>>('/api/empleados?pageSize=100&activo=true'), []);
  const [serverError, setServerError] = useState<string | null>(null);
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<ContratoForm>({
    resolver: zodResolver(contratoSchema),
    defaultValues: { empleadoId: 0, tipo: 'Indeterminado', fechaInicio: todayIso(), fechaFin: '', sueldo: 0 },
  });
  const tipo = watch('tipo');

  const onSubmit = async (v: ContratoForm) => {
    setServerError(null);
    try {
      await api.post('/api/contratos', { ...v, fechaFin: v.fechaFin || null });
      onSaved();
    } catch (e) {
      setServerError(e instanceof ApiError ? e.message : 'Error al guardar');
    }
  };

  return (
    <Modal open title="Nuevo contrato" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert>{serverError}</Alert>}
        <Field label="Empleado" htmlFor="empleadoId" error={errors.empleadoId?.message}>
          <Select id="empleadoId" {...register('empleadoId')}>
            <option value={0}>Seleccione…</option>
            {empleados.data?.items.map((e) => <option key={e.id} value={e.id}>{e.nombreCompleto}</option>)}
          </Select>
        </Field>
        <Field label="Tipo de contrato" htmlFor="tipo">
          <Select id="tipo" {...register('tipo')}>
            {Object.entries(TIPO_CONTRATO_LABEL).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </Select>
        </Field>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Inicio" htmlFor="fechaInicio" error={errors.fechaInicio?.message}><Input id="fechaInicio" type="date" {...register('fechaInicio')} /></Field>
          <Field label={tipo === 'PlazoFijo' ? 'Fin' : 'Fin (opcional)'} htmlFor="fechaFin" error={errors.fechaFin?.message}><Input id="fechaFin" type="date" {...register('fechaFin')} /></Field>
        </div>
        <Field label="Sueldo pactado (S/)" htmlFor="sueldo" error={errors.sueldo?.message}><Input id="sueldo" type="number" step="0.01" {...register('sueldo')} /></Field>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={isSubmitting}>Registrar</Button>
        </div>
      </form>
    </Modal>
  );
}

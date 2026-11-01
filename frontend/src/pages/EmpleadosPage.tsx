import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Pencil, Plus, Search, UserX } from 'lucide-react';
import { api, ApiError, toQuery } from '@/lib/api';
import { useAsync, useDebounce } from '@/lib/hooks';
import { formatCurrency, formatDate, REGIMEN_LABEL } from '@/lib/format';
import { empleadoSchema, type EmpleadoForm } from '@/lib/schemas';
import type { Afp, Catalogo, EmpleadoDetail, EmpleadoListItem, PagedResult } from '@/lib/types';
import { DataTable, Pagination, type Column } from '@/components/DataTable';
import { Alert, Badge, Button, Card, Field, Input, Modal, PageHeader, Select } from '@/components/ui';

export default function EmpleadosPage() {
  const [search, setSearch] = useState('');
  const [areaId, setAreaId] = useState('');
  const [regimen, setRegimen] = useState('');
  const [activo, setActivo] = useState('true');
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<{ by: string; dir: 'asc' | 'desc' }>({ by: 'nombre', dir: 'asc' });
  const [editing, setEditing] = useState<EmpleadoDetail | 'new' | null>(null);
  const debounced = useDebounce(search);

  const areas = useAsync(() => api.get<Catalogo[]>('/api/catalogos/areas'), []);
  const { data, error, loading, reload } = useAsync(
    () => api.get<PagedResult<EmpleadoListItem>>(`/api/empleados${toQuery({
      page, pageSize: 10, search: debounced, areaId, regimen, activo, sortBy: sort.by, sortDir: sort.dir,
    })}`),
    [page, debounced, areaId, regimen, activo, sort],
  );

  // Al cambiar un filtro se vuelve a la primera página.
  useEffect(() => setPage(1), [debounced, areaId, regimen, activo]);

  const abrirEdicion = async (id: number) => setEditing(await api.get<EmpleadoDetail>(`/api/empleados/${id}`));

  const desactivar = async (e: EmpleadoListItem) => {
    if (!confirm(`¿Dar de baja a ${e.nombreCompleto}? Sus boletas se conservarán.`)) return;
    await api.del(`/api/empleados/${e.id}`);
    reload();
  };

  const columns: Column<EmpleadoListItem>[] = [
    {
      key: 'nombre', header: 'Empleado', sortKey: 'nombre',
      render: (e) => (
        <div>
          <p className="font-medium">{e.nombreCompleto}</p>
          <p className="text-xs text-slate-500">{e.email}</p>
        </div>
      ),
    },
    { key: 'dni', header: 'DNI', sortKey: 'dni', render: (e) => <span className="tabular-nums">{e.dni}</span> },
    { key: 'area', header: 'Área / Cargo', sortKey: 'area', render: (e) => <div><p>{e.area}</p><p className="text-xs text-slate-500">{e.cargo}</p></div> },
    { key: 'sueldo', header: 'Sueldo', sortKey: 'sueldo', className: 'text-right', render: (e) => <span className="tabular-nums">{formatCurrency(e.sueldoBasico)}</span> },
    { key: 'regimen', header: 'Pensión', render: (e) => <Badge tone={e.regimen === 'Afp' ? 'blue' : 'green'}>{REGIMEN_LABEL[e.regimen]}{e.afp ? ` · ${e.afp}` : ''}</Badge> },
    { key: 'ingreso', header: 'Ingreso', sortKey: 'ingreso', render: (e) => formatDate(e.fechaIngreso) },
    { key: 'estado', header: 'Estado', render: (e) => <Badge tone={e.activo ? 'green' : 'gray'}>{e.activo ? 'Activo' : 'Inactivo'}</Badge> },
    {
      key: 'acciones', header: '', className: 'text-right',
      render: (e) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" onClick={() => abrirEdicion(e.id)} aria-label={`Editar ${e.nombreCompleto}`}><Pencil className="h-4 w-4" /></Button>
          {e.activo && <Button variant="ghost" onClick={() => desactivar(e)} aria-label={`Dar de baja a ${e.nombreCompleto}`}><UserX className="h-4 w-4" /></Button>}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Empleados"
        description="Datos personales, cargo, remuneración y sistema pensionario."
        actions={<Button onClick={() => setEditing('new')}><Plus className="h-4 w-4" /> Nuevo empleado</Button>}
      />
      <Card>
        <div className="grid gap-3 border-b border-slate-200 p-4 dark:border-slate-800 sm:grid-cols-2 lg:grid-cols-4">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
            <Input className="pl-9" placeholder="Buscar por nombre, DNI o correo" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Buscar" />
          </div>
          <Select value={areaId} onChange={(e) => setAreaId(e.target.value)} aria-label="Filtrar por área">
            <option value="">Todas las áreas</option>
            {areas.data?.map((a) => <option key={a.id} value={a.id}>{a.nombre}</option>)}
          </Select>
          <Select value={regimen} onChange={(e) => setRegimen(e.target.value)} aria-label="Filtrar por régimen">
            <option value="">ONP y AFP</option>
            <option value="Onp">Solo ONP</option>
            <option value="Afp">Solo AFP</option>
          </Select>
          <Select value={activo} onChange={(e) => setActivo(e.target.value)} aria-label="Filtrar por estado">
            <option value="true">Activos</option>
            <option value="false">Inactivos</option>
            <option value="">Todos</option>
          </Select>
        </div>
        <DataTable
          columns={columns}
          rows={data?.items}
          loading={loading}
          error={error}
          onRetry={reload}
          rowKey={(e) => e.id}
          sort={sort}
          onSortChange={(by, dir) => setSort({ by, dir })}
        />
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={data.pageSize} onPageChange={setPage} />}
      </Card>

      {editing && (
        <EmpleadoModal
          empleado={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => { setEditing(null); reload(); }}
        />
      )}
    </>
  );
}

function EmpleadoModal({ empleado, onClose, onSaved }: { empleado: EmpleadoDetail | null; onClose: () => void; onSaved: () => void }) {
  const areas = useAsync(() => api.get<Catalogo[]>('/api/catalogos/areas'), []);
  const cargos = useAsync(() => api.get<Catalogo[]>('/api/catalogos/cargos'), []);
  const afps = useAsync(() => api.get<Afp[]>('/api/catalogos/afps'), []);
  const [serverError, setServerError] = useState<string | null>(null);

  const { register, handleSubmit, watch, setError, formState: { errors, isSubmitting } } = useForm<EmpleadoForm>({
    resolver: zodResolver(empleadoSchema),
    defaultValues: empleado
      ? { ...empleado, afpId: empleado.afpId ?? undefined, cuspp: empleado.cuspp ?? '' }
      : { dni: '', nombres: '', apellidos: '', email: '', fechaIngreso: '', areaId: 0, cargoId: 0, sueldoBasico: 0, tieneHijos: false, regimen: 'Onp', cuspp: '' },
  });
  const regimen = watch('regimen');

  const onSubmit = async (values: EmpleadoForm) => {
    setServerError(null);
    const payload = { ...values, afpId: values.regimen === 'Afp' ? values.afpId : null, cuspp: values.cuspp || null };
    try {
      if (empleado) await api.put(`/api/empleados/${empleado.id}`, payload);
      else await api.post('/api/empleados', payload);
      onSaved();
    } catch (err) {
      if (err instanceof ApiError) {
        // Errores de validación del backend se muestran junto a cada campo.
        Object.entries(err.fieldErrors).forEach(([field, msgs]) => setError(field as keyof EmpleadoForm, { message: msgs[0] }));
        setServerError(err.message);
      }
    }
  };

  return (
    <Modal open title={empleado ? 'Editar empleado' : 'Nuevo empleado'} onClose={onClose} wide>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert>{serverError}</Alert>}
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="DNI" htmlFor="dni" error={errors.dni?.message}><Input id="dni" maxLength={8} inputMode="numeric" {...register('dni')} /></Field>
          <Field label="Correo" htmlFor="email" error={errors.email?.message}><Input id="email" type="email" {...register('email')} /></Field>
          <Field label="Nombres" htmlFor="nombres" error={errors.nombres?.message}><Input id="nombres" {...register('nombres')} /></Field>
          <Field label="Apellidos" htmlFor="apellidos" error={errors.apellidos?.message}><Input id="apellidos" {...register('apellidos')} /></Field>
          <Field label="Área" htmlFor="areaId" error={errors.areaId?.message}>
            <Select id="areaId" {...register('areaId')}>
              <option value={0}>Seleccione…</option>
              {areas.data?.map((a) => <option key={a.id} value={a.id}>{a.nombre}</option>)}
            </Select>
          </Field>
          <Field label="Cargo" htmlFor="cargoId" error={errors.cargoId?.message}>
            <Select id="cargoId" {...register('cargoId')}>
              <option value={0}>Seleccione…</option>
              {cargos.data?.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
            </Select>
          </Field>
          <Field label="Fecha de ingreso" htmlFor="fechaIngreso" error={errors.fechaIngreso?.message}><Input id="fechaIngreso" type="date" {...register('fechaIngreso')} /></Field>
          <Field label="Sueldo básico (S/)" htmlFor="sueldoBasico" error={errors.sueldoBasico?.message}><Input id="sueldoBasico" type="number" step="0.01" {...register('sueldoBasico')} /></Field>
          <Field label="Sistema pensionario" htmlFor="regimen" error={errors.regimen?.message}>
            <Select id="regimen" {...register('regimen')}>
              <option value="Onp">ONP (Sistema Nacional)</option>
              <option value="Afp">AFP (Sistema Privado)</option>
            </Select>
          </Field>
          {regimen === 'Afp' && (
            <>
              <Field label="AFP" htmlFor="afpId" error={errors.afpId?.message}>
                <Select id="afpId" {...register('afpId')}>
                  <option value="">Seleccione…</option>
                  {afps.data?.map((a) => <option key={a.id} value={a.id}>{a.nombre}</option>)}
                </Select>
              </Field>
              <Field label="CUSPP" htmlFor="cuspp" error={errors.cuspp?.message} hint="Código Único del SPP (opcional)"><Input id="cuspp" maxLength={12} {...register('cuspp')} /></Field>
            </>
          )}
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" className="h-4 w-4 rounded border-slate-300" {...register('tieneHijos')} />
          Tiene hijos menores de edad (percibe asignación familiar)
        </label>
        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={isSubmitting}>Guardar</Button>
        </div>
      </form>
    </Modal>
  );
}

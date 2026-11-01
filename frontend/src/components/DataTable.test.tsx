import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DataTable, Pagination, type Column } from './DataTable';
import { ApiError } from '@/lib/api';

interface Fila { id: number; nombre: string }
const columns: Column<Fila>[] = [{ key: 'nombre', header: 'Nombre', sortKey: 'nombre', render: (f) => f.nombre }];

describe('DataTable', () => {
  it('muestra el estado de carga', () => {
    render(<DataTable columns={columns} rows={undefined} loading error={null} rowKey={(f) => f.id} />);
    expect(screen.getByRole('status')).toHaveTextContent(/cargando/i);
  });

  it('muestra el estado vacío', () => {
    render(<DataTable columns={columns} rows={[]} loading={false} error={null} rowKey={(f) => f.id} emptyMessage="Nada por aquí" />);
    expect(screen.getByText('Nada por aquí')).toBeInTheDocument();
  });

  it('muestra el error y permite reintentar', async () => {
    const onRetry = vi.fn();
    render(<DataTable columns={columns} rows={undefined} loading={false} error={new ApiError(500, 'Servidor caído')} onRetry={onRetry} rowKey={(f) => f.id} />);
    expect(screen.getByRole('alert')).toHaveTextContent('Servidor caído');
    await userEvent.click(screen.getByRole('button', { name: /reintentar/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it('renderiza filas y alterna el orden al hacer clic en la cabecera', async () => {
    const onSort = vi.fn();
    render(
      <DataTable
        columns={columns}
        rows={[{ id: 1, nombre: 'Ana' }, { id: 2, nombre: 'Luis' }]}
        loading={false}
        error={null}
        rowKey={(f) => f.id}
        sort={{ by: 'nombre', dir: 'asc' }}
        onSortChange={onSort}
      />,
    );
    expect(screen.getByText('Luis')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: /nombre/i }));
    expect(onSort).toHaveBeenCalledWith('nombre', 'desc');
  });
});

describe('Pagination', () => {
  it('muestra el rango y navega a la página siguiente', async () => {
    const onPage = vi.fn();
    render(<Pagination page={1} totalPages={3} totalCount={25} pageSize={10} onPageChange={onPage} />);
    expect(screen.getByRole('navigation')).toHaveTextContent('Mostrando 1–10 de 25');
    expect(screen.getByRole('button', { name: /anterior/i })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: /siguiente/i }));
    expect(onPage).toHaveBeenCalledWith(2);
  });

  it('no se muestra si no hay registros', () => {
    const { container } = render(<Pagination page={1} totalPages={0} totalCount={0} pageSize={10} onPageChange={() => {}} />);
    expect(container).toBeEmptyDOMElement();
  });
});

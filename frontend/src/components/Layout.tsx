import { useState, type ComponentType } from 'react';
import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import {
  Calculator, CalendarCheck, ClipboardList, FileSignature, FileText, LayoutDashboard, LogOut,
  Menu, Moon, Palmtree, Settings, Sun, User, Users, Wallet, X,
} from 'lucide-react';
import { useAuth } from '@/lib/auth';
import { useTheme } from '@/lib/hooks';
import type { Rol } from '@/lib/types';
import { Spinner } from './ui';

interface NavItem {
  to: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
  roles: Rol[];
}

// El menú se filtra por rol. Ocultar opciones es solo UX: la seguridad real está en la API.
const NAV: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, roles: ['Admin', 'Rrhh'] },
  { to: '/empleados', label: 'Empleados', icon: Users, roles: ['Admin', 'Rrhh'] },
  { to: '/contratos', label: 'Contratos', icon: FileSignature, roles: ['Admin', 'Rrhh'] },
  { to: '/asistencia', label: 'Asistencia', icon: CalendarCheck, roles: ['Admin', 'Rrhh', 'Empleado'] },
  { to: '/vacaciones', label: 'Vacaciones', icon: Palmtree, roles: ['Admin', 'Rrhh', 'Empleado'] },
  { to: '/planillas', label: 'Planillas', icon: ClipboardList, roles: ['Admin', 'Rrhh'] },
  { to: '/mis-boletas', label: 'Mis boletas', icon: Wallet, roles: ['Empleado', 'Rrhh'] },
  { to: '/simulador', label: 'Simulador', icon: Calculator, roles: ['Admin', 'Rrhh'] },
  { to: '/parametros', label: 'Parámetros', icon: Settings, roles: ['Admin', 'Rrhh'] },
  { to: '/perfil', label: 'Mi perfil', icon: User, roles: ['Empleado'] },
];

const ROL_LABEL: Record<Rol, string> = { Admin: 'Administrador', Rrhh: 'Recursos Humanos', Empleado: 'Empleado' };

export function Layout() {
  const { user, logout } = useAuth();
  const { dark, toggle } = useTheme();
  const [open, setOpen] = useState(false);
  const items = NAV.filter((n) => user && n.roles.includes(user.rol));

  const sidebar = (
    <nav className="flex h-full flex-col gap-1 p-4" aria-label="Menú principal">
      <div className="mb-6 flex items-center gap-2 px-2">
        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-white"><FileText className="h-5 w-5" /></div>
        <div>
          <p className="text-sm font-semibold leading-tight">Planillas RR.HH.</p>
          <p className="text-xs text-slate-500 dark:text-slate-400">Empresa Demo S.A.C.</p>
        </div>
      </div>
      {items.map(({ to, label, icon: Icon }) => (
        <NavLink
          key={to}
          to={to}
          end={to === '/'}
          onClick={() => setOpen(false)}
          className={({ isActive }) =>
            `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition ${
              isActive
                ? 'bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200'
                : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
            }`
          }
        >
          <Icon className="h-4 w-4" /> {label}
        </NavLink>
      ))}
    </nav>
  );

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[16rem_1fr]">
      <aside className="hidden border-r border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900 lg:block print:hidden">
        <div className="sticky top-0 h-screen overflow-y-auto">{sidebar}</div>
      </aside>

      {open && (
        <div className="fixed inset-0 z-40 lg:hidden print:hidden">
          <div className="absolute inset-0 bg-slate-900/50" onClick={() => setOpen(false)} />
          <aside className="absolute inset-y-0 left-0 w-64 bg-white shadow-xl dark:bg-slate-900">
            <button className="absolute right-3 top-3 p-1" onClick={() => setOpen(false)} aria-label="Cerrar menú"><X className="h-5 w-5" /></button>
            {sidebar}
          </aside>
        </div>
      )}

      <div className="flex min-w-0 flex-col">
        <header className="sticky top-0 z-30 flex h-16 items-center justify-between gap-3 border-b border-slate-200 bg-white/80 px-4 backdrop-blur dark:border-slate-800 dark:bg-slate-950/80 sm:px-6 print:hidden">
          <button className="rounded-lg p-2 hover:bg-slate-100 dark:hover:bg-slate-800 lg:hidden" onClick={() => setOpen(true)} aria-label="Abrir menú">
            <Menu className="h-5 w-5" />
          </button>
          <div className="ml-auto flex items-center gap-2">
            <button onClick={toggle} className="rounded-lg p-2 hover:bg-slate-100 dark:hover:bg-slate-800" aria-label={dark ? 'Activar modo claro' : 'Activar modo oscuro'}>
              {dark ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
            </button>
            <div className="hidden text-right sm:block">
              <p className="text-sm font-medium leading-tight">{user?.nombreCompleto}</p>
              <p className="text-xs text-slate-500 dark:text-slate-400">{user && ROL_LABEL[user.rol]}</p>
            </div>
            <button onClick={logout} className="rounded-lg p-2 text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800" aria-label="Cerrar sesión" title="Cerrar sesión">
              <LogOut className="h-5 w-5" />
            </button>
          </div>
        </header>
        <main className="mx-auto w-full max-w-7xl flex-1 p-4 sm:p-6 print:p-0">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

/** Protege rutas: exige sesión y, opcionalmente, uno de los roles indicados. */
export function RequireAuth({ roles, children }: { roles?: Rol[]; children: JSX.Element }) {
  const { user, loading } = useAuth();
  const location = useLocation();
  if (loading) return <Spinner label="Verificando sesión…" />;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (roles && !roles.includes(user.rol)) return <Navigate to={homeFor(user.rol)} replace />;
  return children;
}

export const homeFor = (rol: Rol) => (rol === 'Empleado' ? '/mis-boletas' : '/');

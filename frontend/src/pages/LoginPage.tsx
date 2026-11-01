import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { FileText } from 'lucide-react';
import { useAuth } from '@/lib/auth';
import { ApiError } from '@/lib/api';
import { homeFor } from '@/components/Layout';
import { Alert, Button, Field, Input } from '@/components/ui';

const DEMOS = [
  { email: 'admin@demo.pe', rol: 'Administrador' },
  { email: 'rrhh@demo.pe', rol: 'RR.HH.' },
  { email: 'empleado@demo.pe', rol: 'Empleado' },
];

export default function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('admin@demo.pe');
  const [password, setPassword] = useState('Demo123!');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (user) return <Navigate to={homeFor(user.rol)} replace />;

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!email || !password) {
      setError('Ingrese correo y contraseña.');
      return;
    }
    setSubmitting(true);
    try {
      const u = await login(email.trim(), password);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from && from !== '/login' ? from : homeFor(u.rol), { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo iniciar sesión.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-brand-50 to-slate-100 p-4 dark:from-slate-950 dark:to-slate-900">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-xl dark:border-slate-800 dark:bg-slate-900">
        <div className="mb-6 flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-600 text-white"><FileText className="h-6 w-6" /></div>
          <div>
            <h1 className="text-xl font-semibold">Planillas RR.HH.</h1>
            <p className="text-sm text-slate-500 dark:text-slate-400">Gestión de personal y planilla mensual</p>
          </div>
        </div>

        <form onSubmit={onSubmit} className="space-y-4" noValidate>
          {error && <Alert>{error}</Alert>}
          <Field label="Correo electrónico" htmlFor="email">
            <Input id="email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} />
          </Field>
          <Field label="Contraseña" htmlFor="password">
            <Input id="password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </Field>
          <Button type="submit" className="w-full" loading={submitting}>Ingresar</Button>
        </form>

        <div className="mt-6 rounded-lg bg-slate-50 p-4 text-sm dark:bg-slate-800/60">
          <p className="mb-2 font-medium">Cuentas de demostración (contraseña <code>Demo123!</code>)</p>
          <div className="flex flex-wrap gap-2">
            {DEMOS.map((d) => (
              <button key={d.email} type="button" onClick={() => { setEmail(d.email); setPassword('Demo123!'); }}
                className="rounded-md border border-slate-300 px-2 py-1 text-xs hover:bg-white dark:border-slate-600 dark:hover:bg-slate-700">
                {d.rol}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

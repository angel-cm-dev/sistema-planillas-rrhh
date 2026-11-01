import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout, RequireAuth } from './components/Layout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import EmpleadosPage from './pages/EmpleadosPage';
import ContratosPage from './pages/ContratosPage';
import AsistenciaPage from './pages/AsistenciaPage';
import VacacionesPage from './pages/VacacionesPage';
import PlanillasPage from './pages/PlanillasPage';
import PlanillaDetallePage from './pages/PlanillaDetallePage';
import BoletaPage from './pages/BoletaPage';
import MisBoletasPage from './pages/MisBoletasPage';
import SimuladorPage from './pages/SimuladorPage';
import ParametrosPage from './pages/ParametrosPage';
import PerfilPage from './pages/PerfilPage';
import type { Rol } from './lib/types';

const GESTORES: Rol[] = ['Admin', 'Rrhh'];

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth><Layout /></RequireAuth>}>
        <Route index element={<RequireAuth roles={GESTORES}><DashboardPage /></RequireAuth>} />
        <Route path="empleados" element={<RequireAuth roles={GESTORES}><EmpleadosPage /></RequireAuth>} />
        <Route path="contratos" element={<RequireAuth roles={GESTORES}><ContratosPage /></RequireAuth>} />
        <Route path="asistencia" element={<AsistenciaPage />} />
        <Route path="vacaciones" element={<VacacionesPage />} />
        <Route path="planillas" element={<RequireAuth roles={GESTORES}><PlanillasPage /></RequireAuth>} />
        <Route path="planillas/:id" element={<RequireAuth roles={GESTORES}><PlanillaDetallePage /></RequireAuth>} />
        <Route path="boletas/:id" element={<BoletaPage />} />
        <Route path="mis-boletas" element={<MisBoletasPage />} />
        <Route path="simulador" element={<RequireAuth roles={GESTORES}><SimuladorPage /></RequireAuth>} />
        <Route path="parametros" element={<RequireAuth roles={GESTORES}><ParametrosPage /></RequireAuth>} />
        <Route path="perfil" element={<PerfilPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

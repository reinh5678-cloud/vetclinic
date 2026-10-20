import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import RequireAuth, { RequirePermission } from './components/RequireAuth';
import { AuthProvider, useAuth } from './context/AuthContext';
import { BrandingProvider } from './context/BrandingContext';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import PatientsPage from './pages/PatientsPage';
import AppointmentsPage from './pages/AppointmentsPage';
import MedicalHistoryPage from './pages/MedicalHistoryPage';
import HospitalizationsPage from './pages/HospitalizationsPage';
import InventoryIndex from './pages/inventory/InventoryIndex';
import InventoryArticlesPage from './pages/inventory/InventoryArticlesPage';
import LotsPage from './pages/inventory/LotsPage';
import AdjustmentsPage from './pages/inventory/AdjustmentsPage';
import KardexPage from './pages/inventory/KardexPage';
import InvoicesPage from './pages/InvoicesPage';
import ProfilePage from './pages/ProfilePage';
import SettingsPage from './pages/SettingsPage';
import ClinicPanel from './pages/settings/ClinicPanel';
import BillingPanel from './pages/settings/BillingPanel';
import AgendaPanel from './pages/settings/AgendaPanel';
import InventorySettingsPanel from './pages/settings/InventorySettingsPanel';
import SecurityPanel from './pages/settings/SecurityPanel';
import UsersPanel from './pages/settings/UsersPanel';
import RolesPanel from './pages/settings/RolesPanel';
import SystemPanel from './pages/settings/SystemPanel';

const sectionPermissions = [
  'reports.view',
  'patients.view',
  'appointments.view',
  'medical.view',
  'hospitalization.view',
  'inventory.view',
  'invoices.view',
];

function Router() {
  const { can, user } = useAuth();
  const hasAnySection = sectionPermissions.some((p) => can(p));
  const home = hasAnySection ? (
    <DashboardPage />
  ) : (
    <div className="card card-pad text-center">
      <h2 className="page-title">Sin acceso</h2>
      <p className="mt-2 text-sm text-slate-500">Tu usuario no tiene permisos para ver ninguna secciÃ³n. ContactÃ¡ al administrador.</p>
    </div>
  );
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth><Layout /></RequireAuth>}>
        <Route path="/" element={home} />
        <Route path="/perfil" element={<ProfilePage />} />
        <Route path="/pacientes" element={<RequirePermission permission="patients.view"><PatientsPage /></RequirePermission>} />
        <Route path="/agenda" element={<RequirePermission permission="appointments.view"><AppointmentsPage /></RequirePermission>} />
        <Route path="/historia" element={<RequirePermission permission="medical.view"><MedicalHistoryPage /></RequirePermission>} />
        <Route path="/hospitalizacion" element={<RequirePermission permission="hospitalization.view"><HospitalizationsPage /></RequirePermission>} />
                <Route path="/inventario" element={<RequirePermission permission="inventory.view"><InventoryIndex /></RequirePermission>}>
          <Route index element={<InventoryArticlesPage />} />
          <Route path="lotes" element={<LotsPage />} />
          <Route path="ajustes" element={<AdjustmentsPage />} />
          <Route path="kardex" element={<KardexPage />} />
        </Route>
        <Route path="/facturacion" element={<RequirePermission permission="invoices.view"><InvoicesPage /></RequirePermission>} />
        <Route path="/configuracion" element={user?.role === 'Administrador' ? <SettingsPage /> : <Navigate to="/" replace />}>
          <Route index element={<Navigate to="clinica" replace />} />
          <Route path="clinica" element={<ClinicPanel />} />
          <Route path="facturacion" element={<BillingPanel />} />
          <Route path="agenda" element={<AgendaPanel />} />
          <Route path="inventario" element={<InventorySettingsPanel />} />
          <Route path="seguridad" element={<SecurityPanel />} />
          <Route path="usuarios" element={<UsersPanel />} />
          <Route path="roles" element={<RolesPanel />} />
          <Route path="sistema" element={<SystemPanel />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <BrandingProvider>
        <AuthProvider>
          <Router />
        </AuthProvider>
      </BrandingProvider>
    </BrowserRouter>
  );
}


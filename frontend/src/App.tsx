import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import RequireAuth, { RequirePermission } from './components/RequireAuth';
import { AuthProvider, useAuth } from './context/AuthContext';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import PatientsPage from './pages/PatientsPage';
import AppointmentsPage from './pages/AppointmentsPage';
import MedicalHistoryPage from './pages/MedicalHistoryPage';
import HospitalizationsPage from './pages/HospitalizationsPage';
import InventoryPage from './pages/InventoryPage';
import InvoicesPage from './pages/InvoicesPage';
import SettingsPage from './pages/SettingsPage';

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
      <p className="mt-2 text-sm text-slate-500">Tu usuario no tiene permisos para ver ninguna sección. Contactá al administrador.</p>
    </div>
  );
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth><Layout /></RequireAuth>}>
        <Route path="/" element={home} />
        <Route path="/pacientes" element={<RequirePermission permission="patients.view"><PatientsPage /></RequirePermission>} />
        <Route path="/agenda" element={<RequirePermission permission="appointments.view"><AppointmentsPage /></RequirePermission>} />
        <Route path="/historia" element={<RequirePermission permission="medical.view"><MedicalHistoryPage /></RequirePermission>} />
        <Route path="/hospitalizacion" element={<RequirePermission permission="hospitalization.view"><HospitalizationsPage /></RequirePermission>} />
        <Route path="/inventario" element={<RequirePermission permission="inventory.view"><InventoryPage /></RequirePermission>} />
        <Route path="/facturacion" element={<RequirePermission permission="invoices.view"><InvoicesPage /></RequirePermission>} />
        <Route path="/configuracion" element={user?.role === 'Administrador' ? <SettingsPage /> : <Navigate to="/" replace />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Router />
      </AuthProvider>
    </BrowserRouter>
  );
}
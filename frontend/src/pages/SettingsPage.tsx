import { NavLink, Outlet } from 'react-router-dom';

const TABS = [
  { to: 'clinica', label: 'Clínica' },
  { to: 'facturacion', label: 'Facturación' },
  { to: 'agenda', label: 'Agenda' },
  { to: 'inventario', label: 'Inventario' },
  { to: 'seguridad', label: 'Seguridad' },
  { to: 'notificaciones', label: 'Notificaciones' },
  { to: 'usuarios', label: 'Usuarios' },
  { to: 'roles', label: 'Roles' },
  { to: 'sistema', label: 'Sistema' },
];

export default function SettingsPage() {
  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Configuración</h2>
          <p className="page-subtitle">Administración de usuarios, roles y parámetros del sistema.</p>
        </div>
      </div>

      <div className="flex flex-wrap gap-1 border-b border-slate-200">
        {TABS.map((t) => (
          <NavLink
            key={t.to}
            to={t.to}
            className={({ isActive }) =>
              `-mb-px border-b-2 px-4 py-2 text-sm font-medium transition-colors ${
                isActive
                  ? 'border-teal-700 text-teal-800'
                  : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
              }`
            }
          >
            {t.label}
          </NavLink>
        ))}
      </div>

      <Outlet />
    </div>
  );
}

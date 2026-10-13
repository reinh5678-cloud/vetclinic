import { useState } from 'react';
import UsersPanel from './settings/UsersPanel';
import RolesPanel from './settings/RolesPanel';
import SystemPanel from './settings/SystemPanel';

const TABS = [
  { id: 'usuarios', label: 'Usuarios' },
  { id: 'roles', label: 'Roles' },
  { id: 'parametros', label: 'Parámetros' },
] as const;

type TabId = (typeof TABS)[number]['id'];

export default function SettingsPage() {
  const [tab, setTab] = useState<TabId>('usuarios');

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Configuración</h2>
          <p className="page-subtitle">Administración de usuarios, roles y parámetros del sistema.</p>
        </div>
      </div>

      <div className="flex gap-1 border-b border-slate-200">
        {TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`-mb-px cursor-pointer border-b-2 px-4 py-2 text-sm font-medium transition-colors ${
              tab === t.id
                ? 'border-teal-700 text-teal-800'
                : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'usuarios' && <UsersPanel />}
      {tab === 'roles' && <RolesPanel />}
      {tab === 'parametros' && <SystemPanel />}
    </div>
  );
}

import { useSearchParams } from 'react-router-dom';
import PatientsPanel from './patients/PatientsPanel';
import TutorsPanel from './patients/TutorsPanel';
import VaccinesPanel from './patients/VaccinesPanel';

const TABS = [
  { id: 'pacientes', label: 'Pacientes' },
  { id: 'tutores', label: 'Tutores' },
  { id: 'vacunas', label: 'Vacunas y recordatorios' },
] as const;

type TabId = (typeof TABS)[number]['id'];

const isTabId = (value: string | null): value is TabId =>
  value != null && TABS.some((t) => t.id === value);

export default function PatientsPage() {
  // La pestaña vive en la URL para poder llegar con un enlace directo
  // (p. ej. desde la tarjeta "Clientes" del dashboard).
  const [searchParams, setSearchParams] = useSearchParams();
  const tab: TabId = isTabId(searchParams.get('tab')) ? (searchParams.get('tab') as TabId) : 'pacientes';

  const selectTab = (id: TabId) => {
    const p = new URLSearchParams(searchParams);
    p.set('tab', id);
    setSearchParams(p, { replace: true });
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Pacientes</h2>
          <p className="page-subtitle">Mascotas, tutores y seguimiento de vacunas.</p>
        </div>
      </div>

      <div className="flex gap-1 border-b border-slate-200">
        {TABS.map((t) => (
          <button
            key={t.id}
            onClick={() => selectTab(t.id)}
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

      {tab === 'pacientes' && <PatientsPanel />}
      {tab === 'tutores' && <TutorsPanel />}
      {tab === 'vacunas' && <VaccinesPanel />}
    </div>
  );
}

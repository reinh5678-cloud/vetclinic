import { useState, type FormEvent, type JSX } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useBranding } from '../context/BrandingContext';

/* ── SVG icon components ─────────────────────────────────────────── */
const icons: Record<string, JSX.Element> = {
  Dashboard: (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z" />
    </svg>
  ),
  Pacientes: (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 21c-1.5 0-6-3.5-6-8a4 4 0 014-4c.98 0 1.57.4 2 1 .43-.6 1.02-1 2-1a4 4 0 014 4c0 4.5-4.5 8-6 8z" />
      <circle cx="8" cy="5" r="1.5" />
      <circle cx="16" cy="5" r="1.5" />
      <circle cx="5" cy="9" r="1.5" />
      <circle cx="19" cy="9" r="1.5" />
    </svg>
  ),
  Agenda: (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
    </svg>
  ),
  'Historia Clínica': (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15a2.25 2.25 0 012.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25z" />
    </svg>
  ),
  Hospitalización: (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M21 8.25c0-2.485-2.099-4.5-4.688-4.5-1.935 0-3.597 1.126-4.312 2.733-.715-1.607-2.377-2.733-4.313-2.733C5.1 3.75 3 5.765 3 8.25c0 7.22 9 12 9 12s9-4.78 9-12z" />
    </svg>
  ),
  Inventario: (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H2.25c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z" />
    </svg>
  ),
  Facturación: (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 14.25l6-6m4.5-3.493V21.75l-3.75-1.5-3.75 1.5-3.75-1.5-3.75 1.5V4.757c0-1.108.806-2.057 1.907-2.185a48.507 48.507 0 0111.186 0c1.1.128 1.907 1.077 1.907 2.185zM9.75 9h.008v.008H9.75V9zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm4.125 4.5h.008v.008h-.008V13.5zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
    </svg>
  ),
  Configuración: (
    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 011.37.49l1.296 2.247a1.125 1.125 0 01-.26 1.431l-1.003.827c-.293.24-.438.613-.431.992a6.759 6.759 0 010 .255c-.007.378.138.75.43.99l1.005.828c.424.35.534.954.26 1.43l-1.298 2.247a1.125 1.125 0 01-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.57 6.57 0 01-.22.128c-.331.183-.581.495-.644.869l-.213 1.28c-.09.543-.56.941-1.11.941h-2.594c-.55 0-1.02-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 01-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 01-1.369-.49l-1.297-2.247a1.125 1.125 0 01.26-1.431l1.004-.827c.292-.24.437-.613.43-.992a6.932 6.932 0 010-.255c.007-.378-.138-.75-.43-.99l-1.004-.828a1.125 1.125 0 01-.26-1.43l1.297-2.247a1.125 1.125 0 011.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.087.22-.128.332-.183.582-.495.644-.869l.214-1.281z" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
    </svg>
  ),
};

export default function Layout() {
  const { user, logout, can, loginByPin } = useAuth();
  const { name: clinicName, logoDataUrl } = useBranding();
  const navigate = useNavigate();
  const [pin, setPin] = useState('');
  const [pinError, setPinError] = useState('');
  const [showPin, setShowPin] = useState(false);

  const allNav: { to: string; label: string; permission?: string; role?: string }[] = [
    { to: '/', label: 'Dashboard' },
    { to: '/pacientes', label: 'Pacientes', permission: 'patients.view' },
    { to: '/agenda', label: 'Agenda', permission: 'appointments.view' },
    { to: '/historia', label: 'Historia Clínica', permission: 'medical.view' },
    { to: '/hospitalizacion', label: 'Hospitalización', permission: 'hospitalization.view' },
    { to: '/inventario', label: 'Inventario', permission: 'inventory.view' },
    { to: '/facturacion', label: 'Facturación', permission: 'invoices.view' },
    { to: '/configuracion', label: 'Configuración', permission: 'reports.view', role: 'Administrador' },
  ];
  const nav = allNav.filter((n) => (!n.permission || can(n.permission)) && (!n.role || n.role === user?.role));

  const submitPin = async (e: FormEvent) => {
    e.preventDefault();
    setPinError('');
    try {
      await loginByPin(pin.trim());
      setPin('');
      setShowPin(false);
      navigate('/');
    } catch (err) {
      setPinError((err as Error).message);
    }
  };

  /* Build initials for the avatar circle */
  const initials = user?.fullName
    ? user.fullName
        .split(' ')
        .map((w: string) => w[0])
        .join('')
        .slice(0, 2)
        .toUpperCase()
    : '??';

  return (
    <div className="flex min-h-screen">
      {/* ── Sidebar ──────────────────────────────────────────────── */}
      <aside className="sticky top-0 flex h-screen w-64 flex-shrink-0 flex-col bg-gradient-to-b from-teal-950 to-teal-900 text-white">
        {/* Logo area */}
        <div className="px-6 pt-7 pb-5">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center overflow-hidden rounded-xl bg-teal-800/70 ring-1 ring-teal-700/60">
              {logoDataUrl ? (
                <img src={logoDataUrl} alt={clinicName} className="h-full w-full object-cover" />
              ) : (
                <svg className="h-6 w-6 text-teal-300" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 21c-1.5 0-6-3.5-6-8a4 4 0 014-4c.98 0 1.57.4 2 1 .43-.6 1.02-1 2-1a4 4 0 014 4c0 4.5-4.5 8-6 8z" />
                  <circle cx="8" cy="5" r="1.8" />
                  <circle cx="16" cy="5" r="1.8" />
                  <circle cx="5" cy="9" r="1.8" />
                  <circle cx="19" cy="9" r="1.8" />
                </svg>
              )}
            </span>
            <div>
              <span className="block text-lg font-bold leading-tight tracking-tight">{clinicName}</span>
              <span className="block text-xs text-teal-300/70">Sistema de gestión</span>
            </div>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 space-y-1 overflow-y-auto px-3 pb-4">
          {nav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                [
                  'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition-colors',
                  isActive
                    ? 'bg-white/10 text-white font-semibold shadow-sm ring-1 ring-white/10'
                    : 'text-teal-200/80 hover:bg-white/5 hover:text-white',
                ].join(' ')
              }
            >
              {icons[item.label] ?? null}
              {item.label}
            </NavLink>
          ))}
        </nav>

        {/* Bottom: user info + logout */}
        <div className="border-t border-teal-800/70 p-4">
          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full bg-teal-700 text-xs font-bold ring-1 ring-white/10">
              {initials}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium">{user?.fullName}</p>
              <p className="truncate text-xs text-teal-300/70">{user?.role}</p>
            </div>
            <button
              onClick={logout}
              title="Salir"
              className="cursor-pointer rounded-lg p-1.5 text-teal-300/80 transition-colors hover:bg-white/10 hover:text-white"
            >
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
              </svg>
            </button>
          </div>
        </div>
      </aside>

      {/* ── Main content ─────────────────────────────────────────── */}
      <main className="flex min-w-0 flex-1 flex-col">
        {/* Header bar */}
        <header className="sticky top-0 z-10 flex items-center justify-end gap-3 border-b border-slate-200 bg-white/80 px-6 py-3 backdrop-blur">
          <button
            onClick={() => setShowPin((v) => !v)}
            className="btn btn-secondary btn-sm"
          >
            Cambiar por PIN
          </button>
          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-teal-700 text-xs font-bold text-white ring-1 ring-teal-800/20">
            {initials}
          </div>
        </header>

        {/* PIN form */}
        {showPin && (
          <div className="px-6 pt-4">
            <form
              onSubmit={submitPin}
              className="card flex flex-wrap items-center gap-3 px-5 py-3"
            >
              <label htmlFor="quick-pin" className="text-sm font-medium text-slate-700 whitespace-nowrap">
                PIN de acceso:
              </label>
              <input
                id="quick-pin"
                value={pin}
                onChange={(e) => setPin(e.target.value)}
                maxLength={10}
                autoFocus
                className="input w-28"
              />
              <button type="submit" className="btn btn-primary btn-sm">
                Cambiar
              </button>
              {pinError && <span className="text-sm font-medium text-rose-600">{pinError}</span>}
            </form>
          </div>
        )}

        {/* Page content */}
        <div className="flex-1 p-6 lg:p-8">
          <div className="mx-auto w-full max-w-7xl">
            <Outlet />
          </div>
        </div>
      </main>
    </div>
  );
}

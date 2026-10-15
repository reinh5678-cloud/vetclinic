import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useBranding } from '../context/BrandingContext';

export default function LoginPage() {
  const { login } = useAuth();
  const { name: clinicName, logoDataUrl } = useBranding();
  const navigate = useNavigate();
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState('');

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await login(username, password);
      navigate('/');
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const credentials = [
    { u: 'admin', p: 'Admin123!', role: 'Administrador', pin: '1111' },
    { u: 'vet', p: 'Vet123!', role: 'Veterinario', pin: '2222' },
    { u: 'recepcion', p: 'Recep123!', role: 'Recepcionista', pin: '3333' },
  ];

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-gradient-to-br from-teal-950 via-teal-900 to-teal-700 px-4 py-10">
      <div className="pointer-events-none absolute -left-24 -top-24 h-72 w-72 rounded-full bg-teal-500/20 blur-3xl" />
      <div className="pointer-events-none absolute -bottom-24 -right-24 h-72 w-72 rounded-full bg-teal-300/10 blur-3xl" />

      <form
        onSubmit={submit}
        className="relative w-full max-w-sm rounded-2xl border border-white/40 bg-white/95 p-8 shadow-2xl backdrop-blur"
      >
        <div className="mb-5 flex justify-center">
          <div className="flex h-14 w-14 items-center justify-center overflow-hidden rounded-2xl bg-teal-50 ring-1 ring-teal-100">
            {logoDataUrl ? (
              <img src={logoDataUrl} alt={clinicName} className="h-full w-full object-cover" />
            ) : (
              <svg className="h-8 w-8 text-teal-700" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 21c-1.5 0-6-3.5-6-8a4 4 0 014-4c.98 0 1.57.4 2 1 .43-.6 1.02-1 2-1a4 4 0 014 4c0 4.5-4.5 8-6 8z" />
                <circle cx="8" cy="5" r="1.8" />
                <circle cx="16" cy="5" r="1.8" />
                <circle cx="5" cy="9" r="1.8" />
                <circle cx="19" cy="9" r="1.8" />
              </svg>
            )}
          </div>
        </div>

        <h1 className="text-center text-2xl font-bold tracking-tight text-slate-900">{clinicName}</h1>
        <p className="mb-6 text-center text-sm text-slate-500">Inicia sesión para continuar</p>

        {error && (
          <div className="alert alert-error mb-4">{error}</div>
        )}

        <div className="mb-4 field">
          <label htmlFor="login-user" className="label">Usuario</label>
          <input
            id="login-user"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className="input py-2.5"
          />
        </div>

        <div className="mb-6 field">
          <label htmlFor="login-pass" className="label">Contraseña</label>
          <input
            id="login-pass"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="input py-2.5"
          />
        </div>

        <button
          type="submit"
          className="btn btn-primary w-full py-2.5"
        >
          Entrar
        </button>

        <div className="mt-6 rounded-xl border border-slate-200 bg-slate-50 p-4">
          <p className="mb-2 text-xs font-semibold text-slate-600">Usuarios demo:</p>
          <ul className="space-y-1">
            {credentials.map((c) => (
              <li key={c.role} className="text-xs text-slate-500">
                <span className="font-medium text-slate-700">{c.u}</span> / {c.p}{' '}
                <span className="text-slate-400">({c.role})</span> — PIN {c.pin}
              </li>
            ))}
          </ul>
        </div>
      </form>
    </div>
  );
}

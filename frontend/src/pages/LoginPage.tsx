import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useBranding } from '../context/BrandingContext';
import RecoveryModal from '../components/RecoveryModal';

export default function LoginPage() {
  const { login } = useAuth();
  const { name: clinicName, logoDataUrl } = useBranding();
  const navigate = useNavigate();
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('Admin123!!');
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');
  const [showRecovery, setShowRecovery] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setInfo('');
    try {
      await login(username, password);
      navigate('/');
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const credentials = [
    { u: 'admin', p: 'Admin123!!', role: 'Administrador', pin: '1111' },
    { u: 'vet', p: 'Vet123!!', role: 'Veterinario', pin: '2222' },
    { u: 'recepcion', p: 'Recep123!!', role: 'Recepcionista', pin: '3333' },
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
        {info && (
          <div className="alert alert-success mb-4">{info}</div>
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
          <div className="relative">
            <input
              id="login-pass"
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="input py-2.5 pr-11"
            />
            <button
              type="button"
              onClick={() => setShowPassword((v) => !v)}
              aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
              aria-pressed={showPassword}
              title={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
              className="absolute inset-y-0 right-0 flex items-center pr-3 text-slate-400 hover:text-teal-700"
            >
              {showPassword ? (
                <svg className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
                </svg>
              ) : (
                <svg className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              )}
            </button>
          </div>
        </div>

        <button
          type="submit"
          className="btn btn-primary w-full py-2.5"
        >
          Entrar
        </button>

        <button
          type="button"
          onClick={() => { setError(''); setInfo(''); setShowRecovery(true); }}
          className="mt-3 w-full text-center text-sm text-teal-700 hover:underline"
        >
          ¿Olvidaste tu contraseña?
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

      {showRecovery && (
        <RecoveryModal
          onClose={() => setShowRecovery(false)}
          onSuccess={() => {
            setShowRecovery(false);
            setInfo('Contraseña restablecida. Ya puedes iniciar sesión con tu nueva contraseña.');
          }}
        />
      )}
    </div>
  );
}

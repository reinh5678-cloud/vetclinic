import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import {
  getMySecurityQuestions,
  getQuestionCatalog,
  saveSecurityQuestions,
  type SecurityQuestionCatalogItem,
} from '../api/recovery';
import { useAuth } from '../context/AuthContext';

interface UserProfile {
  id: number;
  username: string;
  fullName: string;
  email: string;
  role: string;
  hasPin: boolean;
}

export default function ProfilePage() {
  const { updateUser } = useAuth();

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');

  const [info, setInfo] = useState({ fullName: '', email: '' });
  const [savingInfo, setSavingInfo] = useState(false);

  const [pwd, setPwd] = useState({ current: '', next: '', confirm: '' });
  const [savingPwd, setSavingPwd] = useState(false);

  const [pinForm, setPinForm] = useState({ current: '', next: '' });
  const [savingPin, setSavingPin] = useState(false);

  const [catalog, setCatalog] = useState<SecurityQuestionCatalogItem[]>([]);
  const [myQuestions, setMyQuestions] = useState<SecurityQuestionCatalogItem[]>([]);
  const [sq, setSq] = useState({ currentPassword: '', q1: '', a1: '', q2: '', a2: '' });
  const [savingSq, setSavingSq] = useState(false);

  useEffect(() => {
    api<UserProfile>('/auth/profile')
      .then((p) => {
        setProfile(p);
        setInfo({ fullName: p.fullName, email: p.email });
      })
      .catch((e) => setErr((e as Error).message))
      .finally(() => setLoading(false));

    getQuestionCatalog().then(setCatalog).catch(() => undefined);
    getMySecurityQuestions()
      .then((r) => {
        setMyQuestions(r.questions);
        setSq((s) => ({
          ...s,
          q1: String(r.questions[0]?.questionId ?? ''),
          q2: String(r.questions[1]?.questionId ?? ''),
        }));
      })
      .catch(() => undefined);
  }, []);

  const notify = (text: string) => {
    setErr('');
    setMsg(text);
  };
  const fail = (e: unknown) => {
    setMsg('');
    setErr((e as Error).message);
  };

  const saveInfo = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    setMsg('');
    setSavingInfo(true);
    try {
      const updated = await api<UserProfile>('/auth/profile', {
        method: 'PUT',
        body: JSON.stringify({ fullName: info.fullName.trim(), email: info.email.trim() }),
      });
      setProfile(updated);
      setInfo({ fullName: updated.fullName, email: updated.email });
      updateUser({ id: updated.id, username: updated.username, fullName: updated.fullName, email: updated.email, role: updated.role });
      notify('Datos actualizados.');
    } catch (e) {
      fail(e);
    } finally {
      setSavingInfo(false);
    }
  };

  const changePassword = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    setMsg('');
    if (pwd.next !== pwd.confirm) {
      setErr('La nueva contraseña y su confirmación no coinciden.');
      return;
    }
    setSavingPwd(true);
    try {
      await api('/auth/change-password', {
        method: 'POST',
        body: JSON.stringify({ currentPassword: pwd.current, newPassword: pwd.next }),
      });
      setPwd({ current: '', next: '', confirm: '' });
      notify('Contraseña actualizada.');
    } catch (e) {
      fail(e);
    } finally {
      setSavingPwd(false);
    }
  };

  const changePin = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    setMsg('');
    setSavingPin(true);
    try {
      const updated = await api<UserProfile>('/auth/change-pin', {
        method: 'POST',
        body: JSON.stringify({ currentPin: pinForm.current || null, newPin: pinForm.next.trim() || null }),
      });
      setProfile(updated);
      setPinForm({ current: '', next: '' });
      notify(updated.hasPin ? 'PIN actualizado.' : 'PIN eliminado.');
    } catch (e) {
      fail(e);
    } finally {
      setSavingPin(false);
    }
  };

  const saveQuestions = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    setMsg('');
    if (!sq.q1 || !sq.q2) {
      setErr('Selecciona las dos preguntas de seguridad.');
      return;
    }
    if (sq.q1 === sq.q2) {
      setErr('Las preguntas seleccionadas deben ser distintas.');
      return;
    }
    setSavingSq(true);
    try {
      const result = await saveSecurityQuestions({
        currentPassword: sq.currentPassword,
        questions: [
          { questionId: Number(sq.q1), answer: sq.a1 },
          { questionId: Number(sq.q2), answer: sq.a2 },
        ],
      });
      setMyQuestions(result.questions);
      setSq((s) => ({ ...s, currentPassword: '', a1: '', a2: '' }));
      notify('Preguntas de seguridad actualizadas.');
    } catch (e) {
      fail(e);
    } finally {
      setSavingSq(false);
    }
  };

  if (loading) {
    return (
      <div className="page">
        <p className="text-sm text-slate-400">Cargando…</p>
      </div>
    );
  }
  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Mi perfil</h2>
          <p className="page-subtitle">Administra tus datos personales, contraseña y PIN de acceso.</p>
        </div>
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {profile && (
        <div className="grid gap-6 lg:grid-cols-2">
          {/* Datos personales */}
          <section className="card">
            <div className="card-header">
              <div>
                <h3 className="section-title">Datos personales</h3>
                <p className="mt-0.5 text-xs text-slate-500">Tu nombre aparece en el sistema y en los documentos.</p>
              </div>
            </div>
            <form onSubmit={saveInfo} className="card-body space-y-3">
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="field">
                  <label className="label">Usuario</label>
                  <input value={profile.username} readOnly disabled className="input" />
                </div>
                <div className="field">
                  <label className="label">Rol</label>
                  <input value={profile.role} readOnly disabled className="input" />
                </div>
              </div>
              <div className="field">
                <label className="label">Nombre completo</label>
                <input
                  value={info.fullName}
                  onChange={(e) => setInfo({ ...info, fullName: e.target.value })}
                  placeholder="Nombre completo"
                  className="input"
                />
              </div>
              <div className="field">
                <label className="label">Email</label>
                <input
                  type="email"
                  value={info.email}
                  onChange={(e) => setInfo({ ...info, email: e.target.value })}
                  placeholder="Email"
                  className="input"
                />
              </div>
              <div className="flex justify-end pt-1">
                <button type="submit" className="btn btn-primary btn-sm" disabled={savingInfo}>
                  {savingInfo ? 'Guardando…' : 'Guardar cambios'}
                </button>
              </div>
            </form>
          </section>

          <div className="space-y-6">
            {/* Contraseña */}
            <section className="card">
              <div className="card-header">
                <div>
                  <h3 className="section-title">Contraseña</h3>
                  <p className="mt-0.5 text-xs text-slate-500">Para cambiarla debes confirmar tu contraseña actual.</p>
                </div>
              </div>
              <form onSubmit={changePassword} className="card-body space-y-3">
                <div className="field">
                  <label className="label">Contraseña actual</label>
                  <input
                    type="password"
                    value={pwd.current}
                    onChange={(e) => setPwd({ ...pwd, current: e.target.value })}
                    autoComplete="current-password"
                    required
                    className="input"
                  />
                </div>
                <div className="field">
                  <label className="label">Nueva contraseña</label>
                  <input
                    type="password"
                    value={pwd.next}
                    onChange={(e) => setPwd({ ...pwd, next: e.target.value })}
                    autoComplete="new-password"
                    required
                    className="input"
                  />
                </div>
                <div className="field">
                  <label className="label">Confirmar nueva contraseña</label>
                  <input
                    type="password"
                    value={pwd.confirm}
                    onChange={(e) => setPwd({ ...pwd, confirm: e.target.value })}
                    autoComplete="new-password"
                    required
                    className="input"
                  />
                </div>
                <div className="flex justify-end pt-1">
                  <button type="submit" className="btn btn-primary btn-sm" disabled={savingPwd}>
                    {savingPwd ? 'Guardando…' : 'Cambiar contraseña'}
                  </button>
                </div>
              </form>
            </section>

            {/* PIN */}
            <section className="card">
              <div className="card-header">
                <div>
                  <h3 className="section-title">PIN de acceso</h3>
                  <p className="mt-0.5 text-xs text-slate-500">
                    {profile.hasPin
                      ? 'Tienes un PIN configurado. Puedes cambiarlo o quitarlo.'
                      : 'Aún no tienes PIN. Crea uno para cambiar de usuario rápidamente.'}
                  </p>
                </div>
              </div>
              <form onSubmit={changePin} className="card-body space-y-3">
                {profile.hasPin && (
                  <div className="field">
                    <label className="label">PIN actual</label>
                    <input
                      value={pinForm.current}
                      onChange={(e) => setPinForm({ ...pinForm, current: e.target.value })}
                      inputMode="numeric"
                      maxLength={12}
                      required
                      className="input"
                    />
                  </div>
                )}
                <div className="field">
                  <label className="label">Nuevo PIN {profile.hasPin ? '' : '(déjalo vacío para no configurar)'}</label>
                  <input
                    value={pinForm.next}
                    onChange={(e) => setPinForm({ ...pinForm, next: e.target.value })}
                    inputMode="numeric"
                    maxLength={12}
                    placeholder={profile.hasPin ? 'Vacío para quitar el PIN' : 'Tu PIN'}
                    className="input"
                  />
                </div>
                <div className="flex justify-end pt-1">
                  <button type="submit" className="btn btn-primary btn-sm" disabled={savingPin}>
                    {savingPin ? 'Guardando…' : profile.hasPin ? 'Cambiar PIN' : 'Crear PIN'}
                  </button>
                </div>
              </form>
            </section>

            {/* Preguntas de seguridad */}
            <section className="card">
              <div className="card-header">
                <div>
                  <h3 className="section-title">Preguntas de seguridad</h3>
                  <p className="mt-0.5 text-xs text-slate-500">
                    {myQuestions.length > 0
                      ? 'Ya tienes preguntas configuradas. Puedes reemplazarlas cuando quieras.'
                      : 'Configúralas para poder recuperar tu contraseña sin el administrador.'}
                  </p>
                </div>
              </div>
              <form onSubmit={saveQuestions} className="card-body space-y-3">
                <div className="field">
                  <label className="label">Pregunta 1</label>
                  <select value={sq.q1} onChange={(e) => setSq({ ...sq, q1: e.target.value })} className="select">
                    <option value="">Pregunta…</option>
                    {catalog.map((c) => (
                      <option key={c.questionId} value={c.questionId}>{c.text}</option>
                    ))}
                  </select>
                </div>
                <div className="field">
                  <label className="label">Respuesta 1</label>
                  <input value={sq.a1} onChange={(e) => setSq({ ...sq, a1: e.target.value })} required className="input" />
                </div>
                <div className="field">
                  <label className="label">Pregunta 2</label>
                  <select value={sq.q2} onChange={(e) => setSq({ ...sq, q2: e.target.value })} className="select">
                    <option value="">Pregunta…</option>
                    {catalog.map((c) => (
                      <option key={c.questionId} value={c.questionId}>{c.text}</option>
                    ))}
                  </select>
                </div>
                <div className="field">
                  <label className="label">Respuesta 2</label>
                  <input value={sq.a2} onChange={(e) => setSq({ ...sq, a2: e.target.value })} required className="input" />
                </div>
                <div className="field">
                  <label className="label">Contraseña actual</label>
                  <input
                    type="password"
                    value={sq.currentPassword}
                    onChange={(e) => setSq({ ...sq, currentPassword: e.target.value })}
                    autoComplete="current-password"
                    required
                    className="input"
                  />
                </div>
                <div className="flex justify-end pt-1">
                  <button type="submit" className="btn btn-primary btn-sm" disabled={savingSq}>
                    {savingSq ? 'Guardando…' : 'Guardar preguntas'}
                  </button>
                </div>
              </form>
            </section>
          </div>
        </div>
      )}
    </div>
  );
}

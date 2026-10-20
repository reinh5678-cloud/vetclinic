import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import Modal from '../../components/Modal';

interface AdminUser {
  id: number;
  username: string;
  fullName: string;
  email: string;
  roleId: number;
  role: string;
  isActive: boolean;
  hasPin: boolean;
  hasSecurityQuestions: boolean;
}

interface Role {
  id: number;
  name: string;
}

interface EditState {
  id: number;
  username: string;
  fullName: string;
  email: string;
  roleId: number;
  isActive: boolean;
  isSelf: boolean;
}

export default function UsersPanel() {
  const { user } = useAuth();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({ username: '', password: '', fullName: '', email: '', roleId: '' as number | '', pin: '' });

  const [edit, setEdit] = useState<EditState | null>(null);
  const [resetUser, setResetUser] = useState<AdminUser | null>(null);
  const [resetForm, setResetForm] = useState({ password: '', confirm: '' });
  const [savingReset, setSavingReset] = useState(false);

  const loadUsers = () => api<AdminUser[]>('/settings/users').then(setUsers).catch((e) => setErr((e as Error).message));

  useEffect(() => {
    loadUsers();
    api<Role[]>('/settings/roles').then(setRoles).catch((e) => setErr((e as Error).message));
  }, []);

  const notify = (text: string) => {
    setErr('');
    setMsg(text);
  };

  const openCreate = () => {
    setErr('');
    setMsg('');
    setForm({ username: '', password: '', fullName: '', email: '', roleId: '', pin: '' });
    setShowCreate(true);
  };

  const createUser = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    setMsg('');
    try {
      await api('/settings/users', {
        method: 'POST',
        body: JSON.stringify({
          username: form.username.trim(),
          password: form.password,
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          roleId: form.roleId === '' ? 0 : Number(form.roleId),
          pin: form.pin.trim() || null,
        }),
      });
      setForm({ username: '', password: '', fullName: '', email: '', roleId: '', pin: '' });
      setShowCreate(false);
      notify('Usuario creado.');
      loadUsers();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const startEdit = (u: AdminUser) => {
    setErr('');
    setMsg('');
    setEdit({
      id: u.id,
      username: u.username,
      fullName: u.fullName,
      email: u.email,
      roleId: u.roleId,
      isActive: u.isActive,
      isSelf: u.id === user?.id,
    });
  };

  const saveEdit = async () => {
    if (!edit) return;
    setErr('');
    setMsg('');
    try {
      await api(`/settings/users/${edit.id}`, {
        method: 'PUT',
        body: JSON.stringify({
          fullName: edit.fullName.trim(),
          email: edit.email.trim(),
          roleId: edit.roleId,
          isActive: edit.isActive,
        }),
      });
      setEdit(null);
      notify('Usuario actualizado.');
      loadUsers();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const resetPassword = async (u: AdminUser) => {
    setErr('');
    setMsg('');
    setResetForm({ password: '', confirm: '' });
    setResetUser(u);
  };

  const submitResetPassword = async (e: FormEvent) => {
    e.preventDefault();
    if (!resetUser) return;
    setErr('');
    setMsg('');
    if (resetForm.password !== resetForm.confirm) {
      setErr('La contraseña y su confirmación no coinciden.');
      return;
    }
    setSavingReset(true);
    try {
      await api(`/settings/users/${resetUser.id}/password`, {
        method: 'POST',
        body: JSON.stringify({ password: resetForm.password }),
      });
      notify(`Contraseña actualizada para ${resetUser.username}.`);
      setResetUser(null);
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setSavingReset(false);
    }
  };

  const setPin = async (u: AdminUser) => {
    const pin = window.prompt(`PIN de acceso para ${u.username} (vacío para quitar):`, u.hasPin ? '••••' : '');
    if (pin === null) return;
    setErr('');
    setMsg('');
    try {
      await api(`/settings/users/${u.id}/pin`, { method: 'POST', body: JSON.stringify({ pin: pin.trim() || null }) });
      notify(`PIN actualizado para ${u.username}.`);
      loadUsers();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  return (
    <section className="card">
      {err && !showCreate && !edit && <div className="alert alert-error m-4">{err}</div>}
      {msg && <div className="alert alert-success m-4">{msg}</div>}

      <div className="card-header">
        <div>
          <h3 className="section-title">Usuarios</h3>
          <p className="mt-0.5 text-xs text-slate-500">Cada usuario accede al sistema con un rol que define sus permisos.</p>
        </div>
        <button onClick={openCreate} className="btn btn-primary btn-sm">Nuevo usuario</button>
      </div>

      <div className="card-body">
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Usuario</th>
                <th>Nombre</th>
                <th>Email</th>
                <th>Rol</th>
                <th>Estado</th>
                <th>PIN</th>
                <th>Recuperación</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => {
                const isSelf = u.id === user?.id;
                return (
                  <tr key={u.id}>
                    <td className="font-medium text-slate-800">{u.username}{isSelf && <span className="ml-1 text-xs text-slate-400">(vos)</span>}</td>
                    <td>{u.fullName}</td>
                    <td>{u.email}</td>
                    <td>{u.role}</td>
                    <td>
                      <span className={`badge ${u.isActive ? 'badge-success' : 'badge-neutral'}`}>{u.isActive ? 'Activo' : 'Inactivo'}</span>
                    </td>
                    <td>{u.hasPin ? 'Sí' : '—'}</td>
                    <td>
                      <span className={`badge ${u.hasSecurityQuestions ? 'badge-success' : 'badge-neutral'}`}>
                        {u.hasSecurityQuestions ? 'Preguntas' : '—'}
                      </span>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-2">
                        <button onClick={() => startEdit(u)} className="btn btn-secondary btn-xs">Editar</button>
                        <button onClick={() => resetPassword(u)} className="btn btn-ghost btn-xs">Contraseña</button>
                        <button onClick={() => setPin(u)} className="btn btn-ghost btn-xs">PIN</button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {showCreate && (
        <Modal title="Nuevo usuario" onClose={() => setShowCreate(false)}>
          <form onSubmit={createUser} className="space-y-3">
            {err && <div className="alert alert-error">{err}</div>}
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="field">
                <label className="label">Usuario</label>
                <input required placeholder="Usuario" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Contraseña</label>
                <input required type="password" placeholder="Contraseña (mín. 6)" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Nombre completo</label>
                <input placeholder="Nombre completo" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Email</label>
                <input type="email" placeholder="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Rol</label>
                <select required value={form.roleId} onChange={(e) => setForm({ ...form, roleId: e.target.value === '' ? '' : Number(e.target.value) })} className="select">
                  <option value="">Rol…</option>
                  {roles.map((r) => (
                    <option key={r.id} value={r.id}>{r.name}</option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label className="label">PIN (opcional)</label>
                <input placeholder="PIN (opcional)" value={form.pin} onChange={(e) => setForm({ ...form, pin: e.target.value })} className="input" />
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn btn-secondary" onClick={() => setShowCreate(false)}>Cancelar</button>
              <button type="submit" className="btn btn-primary">Crear usuario</button>
            </div>
          </form>
        </Modal>
      )}

      {resetUser && (
        <Modal title={`Restablecer contraseña · ${resetUser.username}`} onClose={() => setResetUser(null)}>
          <form onSubmit={submitResetPassword} className="space-y-3">
            {err && <div className="alert alert-error">{err}</div>}
            <p className="text-sm text-slate-500">
              Define una nueva contraseña para <span className="font-medium text-slate-700">{resetUser.username}</span>.
            </p>
            <div className="field">
              <label className="label">Nueva contraseña</label>
              <input
                type="password"
                value={resetForm.password}
                onChange={(e) => setResetForm({ ...resetForm, password: e.target.value })}
                autoComplete="new-password"
                required
                className="input"
              />
            </div>
            <div className="field">
              <label className="label">Confirmar nueva contraseña</label>
              <input
                type="password"
                value={resetForm.confirm}
                onChange={(e) => setResetForm({ ...resetForm, confirm: e.target.value })}
                autoComplete="new-password"
                required
                className="input"
              />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn btn-secondary" onClick={() => setResetUser(null)}>Cancelar</button>
              <button type="submit" className="btn btn-primary" disabled={savingReset}>
                {savingReset ? 'Guardando…' : 'Restablecer'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {edit && (
        <Modal title={`Editar usuario · ${edit.username}`} onClose={() => setEdit(null)}>
          <div className="space-y-3">
            {err && <div className="alert alert-error">{err}</div>}
            <div className="field">
              <label className="label">Nombre completo</label>
              <input value={edit.fullName} onChange={(e) => setEdit({ ...edit, fullName: e.target.value })} className="input" />
            </div>
            <div className="field">
              <label className="label">Email</label>
              <input type="email" value={edit.email} onChange={(e) => setEdit({ ...edit, email: e.target.value })} className="input" />
            </div>
            <div className="field">
              <label className="label">Rol</label>
              <select value={edit.roleId} onChange={(e) => setEdit({ ...edit, roleId: Number(e.target.value) })} className="select" disabled={edit.isSelf}>
                {roles.map((r) => (
                  <option key={r.id} value={r.id}>{r.name}</option>
                ))}
              </select>
            </div>
            <label className="flex items-center gap-2 text-sm text-slate-600">
              <input type="checkbox" className="checkbox" checked={edit.isActive} disabled={edit.isSelf} onChange={(e) => setEdit({ ...edit, isActive: e.target.checked })} />
              Activo
            </label>
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn btn-secondary" onClick={() => setEdit(null)}>Cancelar</button>
              <button type="button" className="btn btn-primary" onClick={saveEdit}>Guardar</button>
            </div>
          </div>
        </Modal>
      )}
    </section>
  );
}

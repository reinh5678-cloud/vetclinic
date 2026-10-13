import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../../api/client';
import { useAuth } from '../../context/AuthContext';

interface AdminUser {
  id: number;
  username: string;
  fullName: string;
  email: string;
  roleId: number;
  role: string;
  isActive: boolean;
  hasPin: boolean;
}

interface Role {
  id: number;
  name: string;
}

interface EditState {
  id: number;
  fullName: string;
  email: string;
  roleId: number;
  isActive: boolean;
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

  const loadUsers = () => api<AdminUser[]>('/settings/users').then(setUsers).catch((e) => setErr((e as Error).message));

  useEffect(() => {
    loadUsers();
    api<Role[]>('/settings/roles').then(setRoles).catch((e) => setErr((e as Error).message));
  }, []);

  const notify = (text: string) => {
    setErr('');
    setMsg(text);
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
    setEdit({ id: u.id, fullName: u.fullName, email: u.email, roleId: u.roleId, isActive: u.isActive });
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
    const password = window.prompt(`Nueva contraseña para ${u.username} (mínimo 6 caracteres):`);
    if (password === null) return;
    setErr('');
    setMsg('');
    try {
      await api(`/settings/users/${u.id}/password`, { method: 'POST', body: JSON.stringify({ password }) });
      notify(`Contraseña actualizada para ${u.username}.`);
    } catch (ex) {
      setErr((ex as Error).message);
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
      {err && <div className="alert alert-error m-4">{err}</div>}
      {msg && <div className="alert alert-success m-4">{msg}</div>}

      <div className="card-header">
        <div>
          <h3 className="section-title">Usuarios</h3>
          <p className="mt-0.5 text-xs text-slate-500">Cada usuario accede al sistema con un rol que define sus permisos.</p>
        </div>
        <button onClick={() => setShowCreate((v) => !v)} className={showCreate ? 'btn btn-secondary btn-sm' : 'btn btn-primary btn-sm'}>
          {showCreate ? 'Cancelar' : 'Nuevo usuario'}
        </button>
      </div>

      <div className="card-body space-y-4">
        {showCreate && (
          <form onSubmit={createUser} className="grid gap-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4 sm:grid-cols-2 lg:grid-cols-3">
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
            <div className="sm:col-span-2 lg:col-span-3">
              <button type="submit" className="btn btn-primary">Crear usuario</button>
            </div>
          </form>
        )}

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
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => {
                const isSelf = u.id === user?.id;
                if (edit && edit.id === u.id) {
                  return (
                    <tr key={u.id}>
                      <td className="text-slate-800">{u.username}{isSelf && ' (vos)'}</td>
                      <td><input value={edit.fullName} onChange={(e) => setEdit({ ...edit, fullName: e.target.value })} className="input py-1" /></td>
                      <td><input value={edit.email} onChange={(e) => setEdit({ ...edit, email: e.target.value })} className="input py-1" /></td>
                      <td>
                        <select value={edit.roleId} onChange={(e) => setEdit({ ...edit, roleId: Number(e.target.value) })} className="select py-1" disabled={isSelf}>
                          {roles.map((r) => (
                            <option key={r.id} value={r.id}>{r.name}</option>
                          ))}
                        </select>
                      </td>
                      <td>
                        <label className="flex items-center gap-2 text-xs text-slate-600">
                          <input type="checkbox" className="checkbox" checked={edit.isActive} disabled={isSelf} onChange={(e) => setEdit({ ...edit, isActive: e.target.checked })} />
                          Activo
                        </label>
                      </td>
                      <td>{u.hasPin ? 'Sí' : '—'}</td>
                      <td>
                        <div className="flex gap-2">
                          <button onClick={saveEdit} className="btn btn-primary btn-xs">Guardar</button>
                          <button onClick={() => setEdit(null)} className="btn btn-secondary btn-xs">Cancelar</button>
                        </div>
                      </td>
                    </tr>
                  );
                }
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
    </section>
  );
}

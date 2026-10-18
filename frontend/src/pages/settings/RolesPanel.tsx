import { useEffect, useMemo, useState } from 'react';
import { api } from '../../api/client';
import Modal from '../../components/Modal';
import PermissionPicker, { type Permission } from './PermissionPicker';

interface RoleDetail {
  id: number;
  name: string;
  permissions: string[];
  userCount: number;
  isSystem: boolean;
}

interface Draft {
  id: number | null;
  name: string;
  permissions: string[];
}

export default function RolesPanel() {
  const [roles, setRoles] = useState<RoleDetail[]>([]);
  const [catalog, setCatalog] = useState<Permission[]>([]);
  const [draft, setDraft] = useState<Draft | null>(null);
  const [details, setDetails] = useState<RoleDetail | null>(null);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const loadRoles = () => api<RoleDetail[]>('/settings/roles').then(setRoles).catch((e) => setErr((e as Error).message));

  useEffect(() => {
    loadRoles();
    api<Permission[]>('/settings/permissions').then(setCatalog).catch((e) => setErr((e as Error).message));
  }, []);

  const labels = useMemo(() => {
    const map: Record<string, string> = {};
    catalog.forEach((p) => {
      map[p.code] = p.label;
    });
    return map;
  }, [catalog]);

  const notify = (text: string) => {
    setErr('');
    setMsg(text);
  };

  const startCreate = () => {
    setErr('');
    setMsg('');
    setDraft({ id: null, name: '', permissions: [] });
  };

  const startEdit = (role: RoleDetail) => {
    setErr('');
    setMsg('');
    setDraft({ id: role.id, name: role.name, permissions: [...role.permissions] });
  };

  const toggle = (code: string) =>
    setDraft((d) =>
      d && {
        ...d,
        permissions: d.permissions.includes(code) ? d.permissions.filter((p) => p !== code) : [...d.permissions, code],
      },
    );

  const save = async () => {
    if (!draft) return;
    if (!draft.name.trim()) {
      setErr('El nombre del rol es obligatorio.');
      return;
    }
    setErr('');
    setMsg('');
    try {
      const body = JSON.stringify({ name: draft.name.trim(), permissions: draft.permissions });
      if (draft.id === null) {
        await api('/settings/roles', { method: 'POST', body });
        notify('Rol creado.');
      } else {
        await api(`/settings/roles/${draft.id}`, { method: 'PUT', body });
        notify('Rol actualizado.');
      }
      setDraft(null);
      loadRoles();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  return (
    <section className="card">
      {err && !draft && <div className="alert alert-error m-4">{err}</div>}
      {msg && <div className="alert alert-success m-4">{msg}</div>}

      <div className="card-header">
        <div>
          <h3 className="section-title">Roles</h3>
          <p className="mt-0.5 text-xs text-slate-500">Definen los permisos de los usuarios. El rol Administrador es del sistema.</p>
        </div>
        <button onClick={startCreate} className="btn btn-primary btn-sm">Nuevo rol</button>
      </div>

      <div className="card-body">
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Rol</th>
                <th>Usuarios</th>
                <th>Permisos</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {roles.map((role) => (
                <tr key={role.id}>
                  <td className="font-medium text-slate-800">
                    {role.name}
                    {role.isSystem && <span className="ml-2 badge badge-info">Sistema</span>}
                  </td>
                  <td>{role.userCount}</td>
                  <td>{role.permissions.length}</td>
                  <td>
                    <div className="flex flex-wrap gap-2">
                      <button onClick={() => setDetails(role)} className="btn btn-ghost btn-xs">Detalles</button>
                      <button
                        onClick={() => startEdit(role)}
                        className="btn btn-secondary btn-xs"
                        disabled={role.isSystem}
                        title={role.isSystem ? 'El rol del sistema no puede modificarse' : undefined}
                      >
                        Editar
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {draft && (
        <Modal title={draft.id === null ? 'Nuevo rol' : 'Editar rol'} onClose={() => setDraft(null)} wide>
          <div className="space-y-4">
            {err && <div className="alert alert-error">{err}</div>}
            <div className="field">
              <label className="label">Nombre del rol</label>
              <input
                required
                maxLength={50}
                placeholder="Ej.: Peluquería"
                value={draft.name}
                onChange={(e) => setDraft({ ...draft, name: e.target.value })}
                className="input"
              />
            </div>

            <PermissionPicker catalog={catalog} selected={draft.permissions} onToggle={toggle} />

            <div className="flex items-center justify-between gap-3">
              <span className="text-xs text-slate-500">{draft.permissions.length} permiso(s) seleccionado(s)</span>
              <div className="flex gap-2">
                <button type="button" onClick={() => setDraft(null)} className="btn btn-secondary">Cancelar</button>
                <button type="button" onClick={save} className="btn btn-primary">{draft.id === null ? 'Crear rol' : 'Guardar cambios'}</button>
              </div>
            </div>
          </div>
        </Modal>
      )}

      {details && (
        <Modal title={`Permisos · ${details.name}`} onClose={() => setDetails(null)}>
          {details.permissions.length === 0 ? (
            <p className="text-sm text-slate-500">Este rol no tiene permisos asignados.</p>
          ) : (
            <ul className="flex flex-wrap gap-1.5">
              {details.permissions.map((code) => (
                <li key={code} className="badge badge-neutral">{labels[code] ?? code}</li>
              ))}
            </ul>
          )}
        </Modal>
      )}
    </section>
  );
}

import { useEffect, useState, type FormEvent } from 'react';
import { useAuth } from '../../context/AuthContext';
import {
  createTutor,
  listTutorsWithPets,
  updateTutor,
  type TutorInput,
  type TutorList,
} from '../../api/patients';
import Modal from '../../components/Modal';
import Pagination from '../../components/Pagination';

interface FormState {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  address: string;
  documentNumber: string;
}

const emptyForm: FormState = { firstName: '', lastName: '', email: '', phone: '', address: '', documentNumber: '' };

const PAGE_SIZE = 10;

function toInput(f: FormState): TutorInput {
  return {
    firstName: f.firstName.trim(),
    lastName: f.lastName.trim(),
    email: f.email.trim(),
    phone: f.phone.trim(),
    address: f.address.trim(),
    documentNumber: f.documentNumber.trim() || null,
  };
}

export default function TutorsPanel() {
  const { can } = useAuth();
  const canManage = can('patients.manage');

  const [tutors, setTutors] = useState<TutorList[]>([]);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = () => listTutorsWithPets().then(setTutors).catch((e) => setErr((e as Error).message));
  useEffect(() => { load(); }, []);
  useEffect(() => { setPage(1); }, [search]);

  const filtered = tutors.filter((t) => {
    const term = search.trim().toLowerCase();
    if (!term) return true;
    return `${t.firstName} ${t.lastName} ${t.email} ${t.documentNumber}`.toLowerCase().includes(term);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageStart = (currentPage - 1) * PAGE_SIZE;
  const pagedTutors = filtered.slice(pageStart, pageStart + PAGE_SIZE);

  const notify = (text: string) => { setErr(''); setMsg(text); };

  const openCreate = () => { setEditingId(null); setForm(emptyForm); setErr(''); setMsg(''); setShowForm(true); };
  const openEdit = (t: TutorList) => {
    setEditingId(t.id);
    setForm({ firstName: t.firstName, lastName: t.lastName, email: t.email, phone: t.phone, address: t.address, documentNumber: t.documentNumber });
    setErr(''); setMsg('');
    setShowForm(true);
  };
  const cancelForm = () => { setShowForm(false); setEditingId(null); setForm(emptyForm); };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg('');
    try {
      if (editingId != null) {
        await updateTutor(editingId, toInput(form));
        notify('Tutor actualizado.');
      } else {
        await createTutor(toInput(form));
        notify('Tutor registrado.');
      }
      cancelForm();
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  return (
    <section>
      <div className="card card-pad mb-4 flex flex-wrap items-end gap-3">
        <div className="field min-w-52 flex-1">
          <label className="label">Buscar</label>
          <input placeholder="Nombre, email o documento" value={search} onChange={(e) => setSearch(e.target.value)} className="input" />
        </div>
        {canManage && (
          <button onClick={openCreate} className="btn btn-primary">Nuevo tutor</button>
        )}
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {showForm && canManage && (
        <Modal title={editingId != null ? 'Editar tutor' : 'Nuevo tutor'} onClose={cancelForm}>
          <form onSubmit={submit} className="space-y-3">
            {err && <div className="alert alert-error">{err}</div>}
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="field">
                <label className="label">Nombre *</label>
                <input required placeholder="Nombre" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Apellido *</label>
                <input required placeholder="Apellido" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Documento</label>
                <input placeholder="DNI / CUIT" value={form.documentNumber} onChange={(e) => setForm({ ...form, documentNumber: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Email</label>
                <input type="email" placeholder="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Teléfono</label>
                <input placeholder="Teléfono" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} className="input" />
              </div>
              <div className="field">
                <label className="label">Dirección</label>
                <input placeholder="Dirección" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} className="input" />
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn btn-secondary" onClick={cancelForm}>Cancelar</button>
              <button type="submit" className="btn btn-primary">{editingId != null ? 'Guardar cambios' : 'Registrar tutor'}</button>
            </div>
          </form>
        </Modal>
      )}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Documento</th>
              <th>Email</th>
              <th>Teléfono</th>
              <th className="text-center">Mascotas</th>
              <th className="text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {pagedTutors.map((t) => (
              <tr key={t.id}>
                <td className="font-medium text-slate-800">{t.lastName}, {t.firstName}</td>
                <td>{t.documentNumber || '—'}</td>
                <td>{t.email || '—'}</td>
                <td>{t.phone || '—'}</td>
                <td className="text-center">
                  <span className="badge badge-neutral">{t.petsCount}</span>
                </td>
                <td className="text-center">
                  {canManage && <button onClick={() => openEdit(t)} className="btn btn-ghost btn-xs">Editar</button>}
                </td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td colSpan={6} className="py-10 text-center text-slate-400">No hay tutores registrados.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {filtered.length > 0 && (
        <div className="mt-4">
          <Pagination
            page={currentPage}
            totalItems={filtered.length}
            pageSize={PAGE_SIZE}
            onPageChange={setPage}
          />
        </div>
      )}
    </section>
  );
}

import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { useAuth } from '../../context/AuthContext';
import {
  createPet,
  listPets,
  listTutors,
  setPetActive,
  SPECIES,
  SEXES,
  updatePet,
  type Pet,
  type PetInput,
  type Sex,
  type Species,
  type TutorBasic,
} from '../../api/patients';
import PatientDetail from './PatientDetail';

interface FormState {
  name: string;
  species: Species;
  breed: string;
  birthDate: string;
  sex: Sex;
  weightKg: string;
  color: string;
  tutorId: string;
  isSterilized: boolean;
  photoUrl: string;
}

const emptyForm: FormState = {
  name: '', species: 'Perro', breed: '', birthDate: '', sex: 'Macho',
  weightKg: '1', color: '', tutorId: '', isSterilized: false, photoUrl: '',
};

function toInput(f: FormState): PetInput {
  return {
    name: f.name.trim(),
    species: f.species,
    breed: f.breed.trim(),
    birthDate: f.birthDate ? new Date(f.birthDate).toISOString() : null,
    sex: f.sex,
    weightKg: Number(f.weightKg) || 0,
    color: f.color.trim(),
    tutorId: f.tutorId === '' ? null : Number(f.tutorId),
    isSterilized: f.isSterilized,
    photoUrl: f.photoUrl.trim() || null,
  };
}

export default function PatientsPanel() {
  const { can } = useAuth();
  const canManage = can('patients.manage');

  const [pets, setPets] = useState<Pet[]>([]);
  const [tutors, setTutors] = useState<TutorBasic[]>([]);
  const [search, setSearch] = useState('');
  const [speciesFilter, setSpeciesFilter] = useState('');
  const [activeFilter, setActiveFilter] = useState<'all' | 'active' | 'inactive'>('active');
  const [sort, setSort] = useState<'name' | 'age' | 'weight'>('name');
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);

  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = useCallback(() => {
    listPets({
      search: search.trim() || undefined,
      species: speciesFilter || undefined,
      active: activeFilter === 'all' ? undefined : activeFilter === 'active',
    })
      .then(setPets)
      .catch((e) => setErr((e as Error).message));
  }, [search, speciesFilter, activeFilter]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => { listTutors().then(setTutors).catch(console.error); }, []);

  const sorted = useMemo(() => {
    const list = [...pets];
    if (sort === 'age') list.sort((a, b) => (b.ageYears ?? -1) - (a.ageYears ?? -1));
    else if (sort === 'weight') list.sort((a, b) => b.weightKg - a.weightKg);
    else list.sort((a, b) => a.name.localeCompare(b.name));
    return list;
  }, [pets, sort]);

  const notify = (text: string) => { setErr(''); setMsg(text); };

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setErr(''); setMsg('');
    setShowForm(true);
  };

  const openEdit = (p: Pet) => {
    setEditingId(p.id);
    setForm({
      name: p.name, species: p.species as Species, breed: p.breed,
      birthDate: p.birthDate ? p.birthDate.slice(0, 10) : '', sex: p.sex as Sex,
      weightKg: String(p.weightKg), color: p.color, tutorId: p.tutorId != null ? String(p.tutorId) : '',
      isSterilized: p.isSterilized, photoUrl: p.photoUrl ?? '',
    });
    setErr(''); setMsg('');
    setShowForm(true);
  };

  const cancelForm = () => { setShowForm(false); setEditingId(null); setForm(emptyForm); };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setErr(''); setMsg('');
    try {
      if (editingId != null) {
        await updatePet(editingId, toInput(form));
        notify('Paciente actualizado.');
      } else {
        await createPet(toInput(form));
        notify('Paciente registrado.');
      }
      cancelForm();
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const toggleActive = async (p: Pet) => {
    setErr(''); setMsg('');
    try {
      await setPetActive(p.id, !p.isActive);
      notify(p.isActive ? `${p.name} marcado como inactivo.` : `${p.name} reactivado.`);
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
          <input placeholder="Nombre, raza o tutor" value={search} onChange={(e) => setSearch(e.target.value)} className="input" />
        </div>
        <div className="field w-36">
          <label className="label">Especie</label>
          <select value={speciesFilter} onChange={(e) => setSpeciesFilter(e.target.value)} className="select">
            <option value="">Todas</option>
            {SPECIES.map((s) => <option key={s}>{s}</option>)}
          </select>
        </div>
        <div className="field w-36">
          <label className="label">Estado</label>
          <select value={activeFilter} onChange={(e) => setActiveFilter(e.target.value as typeof activeFilter)} className="select">
            <option value="active">Activos</option>
            <option value="inactive">Inactivos</option>
            <option value="all">Todos</option>
          </select>
        </div>
        <div className="field w-40">
          <label className="label">Ordenar por</label>
          <select value={sort} onChange={(e) => setSort(e.target.value as typeof sort)} className="select">
            <option value="name">Nombre</option>
            <option value="age">Edad</option>
            <option value="weight">Peso</option>
          </select>
        </div>
        {canManage && (
          <button onClick={showForm ? cancelForm : openCreate} className={showForm ? 'btn btn-secondary' : 'btn btn-primary'}>
            {showForm ? 'Cancelar' : 'Nuevo paciente'}
          </button>
        )}
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {showForm && canManage && (
        <form onSubmit={submit} className="card card-pad mb-4 space-y-3">
          <h3 className="section-title">{editingId != null ? 'Editar paciente' : 'Nuevo paciente'}</h3>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <div className="field">
              <label className="label">Nombre *</label>
              <input required placeholder="Nombre" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className="input" />
            </div>
            <div className="field">
              <label className="label">Especie</label>
              <select value={form.species} onChange={(e) => setForm({ ...form, species: e.target.value as Species })} className="select">
                {SPECIES.map((s) => <option key={s}>{s}</option>)}
              </select>
            </div>
            <div className="field">
              <label className="label">Raza</label>
              <input placeholder="Raza" value={form.breed} onChange={(e) => setForm({ ...form, breed: e.target.value })} className="input" />
            </div>
            <div className="field">
              <label className="label">Fecha de nacimiento</label>
              <input type="date" value={form.birthDate} onChange={(e) => setForm({ ...form, birthDate: e.target.value })} className="input" />
            </div>
            <div className="field">
              <label className="label">Sexo</label>
              <select value={form.sex} onChange={(e) => setForm({ ...form, sex: e.target.value as Sex })} className="select">
                {SEXES.map((s) => <option key={s}>{s}</option>)}
              </select>
            </div>
            <div className="field">
              <label className="label">Peso (kg)</label>
              <input type="number" min="0" step="0.1" value={form.weightKg} onChange={(e) => setForm({ ...form, weightKg: e.target.value })} className="input" />
            </div>
            <div className="field">
              <label className="label">Color</label>
              <input placeholder="Color" value={form.color} onChange={(e) => setForm({ ...form, color: e.target.value })} className="input" />
            </div>
            <div className="field">
              <label className="label">Tutor</label>
              <select value={form.tutorId} onChange={(e) => setForm({ ...form, tutorId: e.target.value })} className="select">
                <option value="">Sin tutor</option>
                {tutors.map((t) => <option key={t.id} value={t.id}>{t.firstName} {t.lastName}</option>)}
              </select>
            </div>
            <div className="field">
              <label className="label">URL de foto</label>
              <input placeholder="https://..." value={form.photoUrl} onChange={(e) => setForm({ ...form, photoUrl: e.target.value })} className="input" />
            </div>
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" className="checkbox" checked={form.isSterilized} onChange={(e) => setForm({ ...form, isSterilized: e.target.checked })} />
            Esterilizado
          </label>
          <button type="submit" className="btn btn-primary">{editingId != null ? 'Guardar cambios' : 'Registrar paciente'}</button>
        </form>
      )}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Foto</th>
              <th>Nombre</th>
              <th className="text-center">Especie</th>
              <th className="text-center">Raza</th>
              <th className="text-center">Sexo</th>
              <th className="text-center">Edad</th>
              <th className="text-center">Peso</th>
              <th className="text-center">Tutor</th>
              <th className="text-center">Estado</th>
              <th className="text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((p) => (
              <PetRow
                key={p.id}
                pet={p}
                expanded={expandedId === p.id}
                canManage={canManage}
                onToggle={() => setExpandedId(expandedId === p.id ? null : p.id)}
                onEdit={() => openEdit(p)}
                onToggleActive={() => toggleActive(p)}
              />
            ))}
            {sorted.length === 0 && (
              <tr>
                <td colSpan={10} className="py-10 text-center text-slate-400">No hay pacientes que coincidan con los filtros.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

interface PetRowProps {
  pet: Pet;
  expanded: boolean;
  canManage: boolean;
  onToggle: () => void;
  onEdit: () => void;
  onToggleActive: () => void;
}

function PetRow({ pet, expanded, canManage, onToggle, onEdit, onToggleActive }: PetRowProps) {
  return (
    <>
      <tr className={pet.isActive ? undefined : 'opacity-60'}>
        <td>
          {pet.photoUrl
            ? <img src={pet.photoUrl} alt={pet.name} className="inline-block h-10 w-10 rounded-lg object-cover ring-1 ring-slate-200" onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }} />
            : <span className="text-slate-300">—</span>}
        </td>
        <td className="font-medium text-slate-800">{pet.name}</td>
        <td className="text-center">{pet.species}</td>
        <td className="text-center">{pet.breed || '—'}</td>
        <td className="text-center">{pet.sex}</td>
        <td className="text-center">{pet.ageYears != null ? pet.ageYears : '—'}</td>
        <td className="text-center">{pet.weightKg} kg</td>
        <td className="text-center">{pet.tutorName ?? '—'}</td>
        <td className="text-center">
          {pet.isActive
            ? <span className="badge badge-success">Activo</span>
            : <span className="badge badge-neutral">Inactivo</span>}
        </td>
        <td className="text-center">
          <div className="flex flex-wrap justify-center gap-2">
            <button onClick={onToggle} className="btn btn-secondary btn-xs">{expanded ? 'Ocultar' : 'Ficha'}</button>
            {canManage && <button onClick={onEdit} className="btn btn-ghost btn-xs">Editar</button>}
            {canManage && (
              <button onClick={onToggleActive} className="btn btn-ghost btn-xs">
                {pet.isActive ? 'Desactivar' : 'Activar'}
              </button>
            )}
          </div>
        </td>
      </tr>
      {expanded && (
        <tr className="hover:bg-transparent">
          <td colSpan={10} className="bg-slate-50/70 p-4">
            <PatientDetail pet={pet} />
          </td>
        </tr>
      )}
    </>
  );
}

import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import {
  addVaccine,
  listPets,
  listUpcomingVaccines,
  type Pet,
  type UpcomingVaccine,
} from '../../api/patients';

interface InventoryItem { id: number; name: string; category: string; }

const DAY_OPTIONS = [7, 30, 90, 365];

export default function VaccinesPanel() {
  const { can } = useAuth();
  const canManage = can('patients.manage');

  const [days, setDays] = useState(30);
  const [upcoming, setUpcoming] = useState<UpcomingVaccine[]>([]);
  const [pets, setPets] = useState<Pet[]>([]);
  const [items, setItems] = useState<InventoryItem[]>([]);

  const [petId, setPetId] = useState('');
  const [itemId, setItemId] = useState('');
  const [applicationDate, setApplicationDate] = useState(new Date().toISOString().slice(0, 10));
  const [nextDueDate, setNextDueDate] = useState('');

  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = useCallback(() => {
    listUpcomingVaccines(days).then(setUpcoming).catch((e) => setErr((e as Error).message));
  }, [days]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => {
    listPets({ active: true }).then(setPets).catch(console.error);
    api<InventoryItem[]>('/inventory')
      .then((all) => setItems(all.filter((i) => i.category.toLowerCase() === 'vacunas')))
      .catch(console.error);
  }, []);

  const overdue = upcoming.filter((v) => v.isOverdue);

  const notify = (text: string) => { setErr(''); setMsg(text); };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !itemId) return;
    setErr(''); setMsg('');
    try {
      await addVaccine(Number(petId), {
        inventoryItemId: Number(itemId),
        applicationDate: new Date(applicationDate).toISOString(),
        nextDueDate: nextDueDate ? new Date(nextDueDate).toISOString() : null,
      });
      setItemId(''); setNextDueDate('');
      notify('Vacuna registrada.');
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  return (
    <section>
      <div className="card card-pad mb-4 flex flex-wrap items-end gap-3">
        <div className="field w-44">
          <label className="label">Próximos (días)</label>
          <select value={days} onChange={(e) => setDays(Number(e.target.value))} className="select">
            {DAY_OPTIONS.map((d) => <option key={d} value={d}>{d} días</option>)}
          </select>
        </div>
        {overdue.length > 0 && (
          <div className="alert alert-error mb-0 flex-1">
            <span>{overdue.length === 1 ? 'Hay 1 dosis vencida' : `Hay ${overdue.length} dosis vencidas`} que requieren seguimiento.</span>
          </div>
        )}
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {canManage && (
        <form onSubmit={submit} className="card card-pad mb-4">
          <h3 className="section-title mb-3">Registrar vacuna</h3>
          <div className="flex flex-wrap items-end gap-3">
            <div className="field min-w-48 flex-1">
              <label className="label">Paciente</label>
              <select value={petId} onChange={(e) => setPetId(e.target.value)} className="select" required>
                <option value="">-- Paciente --</option>
                {pets.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.species})</option>)}
              </select>
            </div>
            <div className="field min-w-48 flex-1">
              <label className="label">Vacuna</label>
              <select value={itemId} onChange={(e) => setItemId(e.target.value)} className="select" required>
                <option value="">-- Vacuna --</option>
                {items.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
              </select>
            </div>
            <div className="field">
              <label className="label">Aplicación</label>
              <input type="date" value={applicationDate} onChange={(e) => setApplicationDate(e.target.value)} className="input" required />
            </div>
            <div className="field">
              <label className="label">Próxima dosis</label>
              <input type="date" value={nextDueDate} onChange={(e) => setNextDueDate(e.target.value)} className="input" />
            </div>
            <button type="submit" className="btn btn-primary">Registrar</button>
          </div>
        </form>
      )}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Paciente</th>
              <th>Tutor</th>
              <th>Vacuna</th>
              <th className="text-center">Aplicada</th>
              <th className="text-center">Próxima dosis</th>
              <th className="text-center">Estado</th>
            </tr>
          </thead>
          <tbody>
            {upcoming.map((v, i) => (
              <tr key={`${v.petId}-${v.nextDueDate}-${i}`}>
                <td className="font-medium text-slate-800">{v.petName}</td>
                <td>{v.tutorName ?? '—'}</td>
                <td>{v.vaccineName ?? 'Vacuna'}</td>
                <td className="text-center">{new Date(v.applicationDate).toLocaleDateString()}</td>
                <td className="text-center">{new Date(v.nextDueDate).toLocaleDateString()}</td>
                <td className="text-center">
                  {v.isOverdue
                    ? <span className="badge badge-danger">VENCIDA</span>
                    : <span className="badge badge-warning">PRÓXIMA</span>}
                </td>
              </tr>
            ))}
            {upcoming.length === 0 && (
              <tr>
                <td colSpan={6} className="py-10 text-center text-slate-400">Sin dosis pendientes en los próximos {days} días.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

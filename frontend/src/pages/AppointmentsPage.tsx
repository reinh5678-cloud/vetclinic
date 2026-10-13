import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';

interface Appointment {
  id: number; startTime: string; urgency: string; status: string; reason: string; petName: string | null; vetName: string | null;
}

interface Pet { id: number; name: string; species: string; breed: string; }
interface UserLite { id: number; fullName: string; role: string; }

const urgencies = ['Baja', 'Media', 'Alta', 'Emergencia'];

const transitions: Record<string, string[]> = {
  Programada: ['EnEspera', 'EnConsulta', 'Completada', 'Cancelada', 'NoAsistio'],
  EnEspera: ['EnConsulta', 'Completada', 'Cancelada', 'NoAsistio'],
  EnConsulta: ['Completada', 'Cancelada'],
  Completada: [],
  Cancelada: [],
  NoAsistio: ['Programada'],
};

const statusBadge: Record<string, string> = {
  Programada: 'badge-info',
  EnEspera: 'badge-warning',
  EnConsulta: 'badge-info',
  Completada: 'badge-success',
  Cancelada: 'badge-neutral',
  NoAsistio: 'badge-danger',
};

export default function AppointmentsPage() {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [reason, setReason] = useState('');
  const [pets, setPets] = useState<Pet[]>([]);
  const [vets, setVets] = useState<UserLite[]>([]);
  const [petId, setPetId] = useState('');
  const [vetId, setVetId] = useState('');
  const [urgency, setUrgency] = useState('Media');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = () => api<Appointment[]>('/appointments/upcoming').then(setAppointments).catch(console.error);
  useEffect(() => {
    load();
    api<Pet[]>('/patients/pets').then(setPets).catch(console.error);
    api<UserLite[]>('/auth/users')
      .then((users) => setVets(users.filter((u) => u.role === 'Veterinario')))
      .catch(console.error);
  }, []);

  const create = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    try {
      const start = new Date();
      start.setDate(start.getDate() + 1);
      start.setHours(9, 0, 0, 0);
      await api('/appointments', {
        method: 'POST',
        body: JSON.stringify({
          startTime: start.toISOString(),
          durationMinutes: 30,
          urgency,
          reason,
          petId: petId ? Number(petId) : null,
          vetId: vetId ? Number(vetId) : null,
        }),
      });
      setReason('');
      setMsg('Cita agendada.');
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const setStatus = async (id: number, status: string) => {
    setErr('');
    try {
      await api(`/appointments/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) });
      setMsg(`Estado actualizado a «${status}».`);
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Agenda</h2>
          <p className="page-subtitle">Programación y seguimiento de citas por paciente y veterinario.</p>
        </div>
      </div>

      <form onSubmit={create} className="card card-pad flex flex-wrap items-end gap-3">
        <div className="field min-w-44">
          <label className="label">Paciente</label>
          <select value={petId} onChange={(e) => setPetId(e.target.value)} className="select" required>
            <option value="">-- Paciente --</option>
            {pets.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.species})</option>)}
          </select>
        </div>
        <div className="field min-w-44">
          <label className="label">Veterinario</label>
          <select value={vetId} onChange={(e) => setVetId(e.target.value)} className="select" required>
            <option value="">-- Veterinario --</option>
            {vets.map((v) => <option key={v.id} value={v.id}>{v.fullName}</option>)}
          </select>
        </div>
        <div className="field w-36">
          <label className="label">Urgencia</label>
          <select value={urgency} onChange={(e) => setUrgency(e.target.value)} className="select">
            {urgencies.map((u) => <option key={u}>{u}</option>)}
          </select>
        </div>
        <div className="field min-w-56 flex-1">
          <label className="label">Motivo</label>
          <input
            placeholder="Motivo de la cita (ej: control, vacunación)"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="input"
            required
          />
        </div>
        <button type="submit" className="btn btn-primary">Agendar</button>
      </form>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th className="text-center">Paciente</th>
              <th className="text-center">Veterinario</th>
              <th>Motivo</th>
              <th className="text-center">Estado</th>
              <th className="text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {appointments.map((a) => (
              <tr key={a.id}>
                <td className="whitespace-nowrap">{new Date(a.startTime).toLocaleString()}</td>
                <td className="text-center">{a.petName ?? '-'}</td>
                <td className="text-center">{a.vetName ?? '-'}</td>
                <td>{a.reason}</td>
                <td className="text-center">
                  <span className={`badge ${statusBadge[a.status] ?? 'badge-neutral'}`}>{a.status}</span>
                </td>
                <td className="text-center">
                  <select
                    value={a.status}
                    onChange={(e) => setStatus(a.id, e.target.value)}
                    className="select mx-auto w-auto py-1 text-xs"
                  >
                    {[a.status, ...(transitions[a.status] ?? [])].map((s) => <option key={s}>{s}</option>)}
                  </select>
                </td>
              </tr>
            ))}
            {appointments.length === 0 && (
              <tr>
                <td colSpan={6} className="py-10 text-center text-slate-400">No hay citas programadas.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="alert alert-info">
        <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>Los cambios de estado se validan contra las transiciones permitidas; el cruce de horarios (overlap) se valida en el backend al agendar.</p>
      </div>
    </div>
  );
}

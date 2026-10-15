import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../context/AuthContext';
import RescheduleDialog from './appointments/RescheduleDialog';
import {
  STATUS_BADGE,
  STATUS_TRANSITIONS,
  TERMINAL_STATUSES,
  URGENCIES,
  createAppointment,
  defaultStartLocal,
  fromDateTimeLocal,
  listAppointments,
  updateAppointmentStatus,
  type Appointment,
  type AppointmentFilters,
  type AppointmentStatus,
  type UrgencyLevel,
} from '../api/appointments';

interface Pet { id: number; name: string; species: string; }
interface UserLite { id: number; fullName: string; role: string; }

type View = 'today' | 'upcoming' | 'all';
const VIEWS: { id: View; label: string }[] = [
  { id: 'today', label: 'Hoy' },
  { id: 'upcoming', label: 'Próximas' },
  { id: 'all', label: 'Todas' },
];

export default function AppointmentsPage() {
  const { can } = useAuth();
  const navigate = useNavigate();
  const canView = can('appointments.view');
  const canManage = can('appointments.manage');
  const canManageMedical = can('medical.manage');

  const [view, setView] = useState<View>('upcoming');
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [pets, setPets] = useState<Pet[]>([]);
  const [vets, setVets] = useState<UserLite[]>([]);

  const [filterVet, setFilterVet] = useState('');
  const [filterStatus, setFilterStatus] = useState('');

  const [petId, setPetId] = useState('');
  const [vetId, setVetId] = useState('');
  const [urgency, setUrgency] = useState('Media');
  const [reason, setReason] = useState('');
  const [startLocal, setStartLocal] = useState(defaultStartLocal());
  const [duration, setDuration] = useState('30');

  const [editing, setEditing] = useState<Appointment | null>(null);
  const [cancelTarget, setCancelTarget] = useState<Appointment | null>(null);
  const [cancelNote, setCancelNote] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = useCallback(() => {
    if (!canView) return;
    const filters: AppointmentFilters = {};
    if (view === 'today') {
      const start = new Date();
      start.setHours(0, 0, 0, 0);
      const end = new Date(start);
      end.setDate(end.getDate() + 1);
      filters.from = start.toISOString();
      filters.to = end.toISOString();
    } else if (view === 'upcoming') {
      filters.from = new Date().toISOString();
    }
    if (filterVet) filters.vetId = Number(filterVet);
    if (filterStatus) filters.status = filterStatus;

    listAppointments(filters).then(setAppointments).catch((e) => setErr((e as Error).message));
  }, [canView, view, filterVet, filterStatus]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => {
    if (!canView) return;
    api<Pet[]>('/patients/pets')
      .then(setPets)
      .catch(console.error);
    api<UserLite[]>('/auth/users')
      .then((users) => setVets(users.filter((u) => u.role === 'Veterinario')))
      .catch(console.error);
  }, [canView]);

  const notify = (text: string) => { setErr(''); setMsg(text); };

  const create = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !vetId || !startLocal) return;
    setErr('');
    try {
      await createAppointment({
        startTime: fromDateTimeLocal(startLocal),
        durationMinutes: Number(duration),
        urgency: urgency as UrgencyLevel,
        reason,
        petId: Number(petId),
        vetId: Number(vetId),
      });
      setReason('');
      setStartLocal(defaultStartLocal());
      notify('Cita agendada.');
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const changeStatus = async (a: Appointment, status: AppointmentStatus) => {
    if (status === a.status) return;
    if (status === 'Cancelada') {
      setCancelTarget(a);
      setCancelNote('');
      return;
    }
    setErr('');
    try {
      await updateAppointmentStatus(a.id, status);
      notify(`Estado actualizado a «${status}».`);
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const confirmCancel = async () => {
    if (!cancelTarget) return;
    setErr('');
    try {
      await updateAppointmentStatus(cancelTarget.id, 'Cancelada', cancelNote.trim() || undefined);
      setCancelTarget(null);
      setCancelNote('');
      notify('Cita cancelada.');
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  if (!canView) {
    return (
      <div className="page">
        <div className="page-header">
          <div>
            <h2 className="page-title">Agenda</h2>
            <p className="page-subtitle">Programación y seguimiento de citas por paciente y veterinario.</p>
          </div>
        </div>
        <div className="alert alert-warning">No tienes permiso para ver la agenda.</div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Agenda</h2>
          <p className="page-subtitle">Programación y seguimiento de citas por paciente y veterinario.</p>
        </div>
        <div className="flex rounded-lg border border-slate-200 bg-white p-1">
          {VIEWS.map((v) => (
            <button
              key={v.id}
              type="button"
              onClick={() => setView(v.id)}
              className={
                'rounded-md px-3 py-1.5 text-sm font-medium transition-colors ' +
                (view === v.id ? 'bg-teal-700 text-white' : 'text-slate-600 hover:bg-slate-100')
              }
            >
              {v.label}
            </button>
          ))}
        </div>
      </div>

      <div className="card card-pad flex flex-wrap items-end gap-3">
        <div className="field min-w-44">
          <label className="label">Veterinario</label>
          <select value={filterVet} onChange={(e) => setFilterVet(e.target.value)} className="select">
            <option value="">Todos</option>
            {vets.map((v) => <option key={v.id} value={v.id}>{v.fullName}</option>)}
          </select>
        </div>
        <div className="field min-w-44">
          <label className="label">Estado</label>
          <select value={filterStatus} onChange={(e) => setFilterStatus(e.target.value)} className="select">
            <option value="">Todos</option>
            {Object.keys(STATUS_TRANSITIONS).map((s) => <option key={s}>{s}</option>)}
          </select>
        </div>
        <div className="flex-1" />
        <div className="text-sm text-slate-500">
          {appointments.length} {appointments.length === 1 ? 'cita' : 'citas'}
        </div>
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {canManage && (
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
          <div className="field">
            <label className="label">Fecha y hora</label>
            <input
              type="datetime-local"
              value={startLocal}
              onChange={(e) => setStartLocal(e.target.value)}
              className="input"
              required
            />
          </div>
          <div className="field w-28">
            <label className="label">Duración (min)</label>
            <input
              type="number"
              min={5}
              max={480}
              step={5}
              value={duration}
              onChange={(e) => setDuration(e.target.value)}
              className="input"
              required
            />
          </div>
          <div className="field w-36">
            <label className="label">Urgencia</label>
            <select value={urgency} onChange={(e) => setUrgency(e.target.value)} className="select">
              {URGENCIES.map((u) => <option key={u}>{u}</option>)}
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
      )}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th className="text-center">Paciente</th>
              <th className="text-center">Veterinario</th>
              <th className="text-center">Urgencia</th>
              <th>Motivo</th>
              <th className="text-center">Estado</th>
              {canManage && <th className="text-center">Acciones</th>}
            </tr>
          </thead>
          <tbody>
            {appointments.map((a) => {
              const terminal = TERMINAL_STATUSES.includes(a.status);
              const targets = [a.status, ...(STATUS_TRANSITIONS[a.status] ?? [])];
              return (
                <tr key={a.id}>
                  <td className="whitespace-nowrap">
                    <div>{new Date(a.startTime).toLocaleString()}</div>
                    <div className="text-xs text-slate-400">{a.durationMinutes} min</div>
                  </td>
                  <td className="text-center">{a.petName ?? '-'}</td>
                  <td className="text-center">{a.vetName ?? '-'}</td>
                  <td className="text-center">{a.urgency}</td>
                  <td>
                    <div>{a.reason}</div>
                    {a.notes && <div className="text-xs text-slate-400">{a.notes}</div>}
                  </td>
                  <td className="text-center">
                    <span className={`badge ${STATUS_BADGE[a.status] ?? 'badge-neutral'}`}>{a.status}</span>
                  </td>
                  {canManage && (
                    <td className="text-center">
                      <div className="flex items-center justify-center gap-2">
                        {canManageMedical && a.petId && !terminal && (
                          <button
                            type="button"
                            className="btn btn-primary btn-xs"
                            title="Registrar la consulta de esta cita"
                            onClick={() => navigate(`/historia?petId=${a.petId}&appointmentId=${a.id}`)}
                          >
                            Consulta
                          </button>
                        )}
                        <button
                          type="button"
                          className="btn btn-secondary btn-xs"
                          disabled={terminal}
                          onClick={() => setEditing(a)}
                        >
                          Reprogramar
                        </button>
                        <select
                          value={a.status}
                          onChange={(e) => changeStatus(a, e.target.value as AppointmentStatus)}
                          className="select w-auto py-1 text-xs"
                        >
                          {targets.map((s) => <option key={s}>{s}</option>)}
                        </select>
                      </div>
                    </td>
                  )}
                </tr>
              );
            })}
            {appointments.length === 0 && (
              <tr>
                <td colSpan={canManage ? 7 : 6} className="py-10 text-center text-slate-400">
                  No hay citas para este filtro.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="alert alert-info">
        <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>Los cambios de estado se validan contra las transiciones permitidas; el cruce de horarios (overlap) se valida en el backend al agendar o reprogramar.</p>
      </div>

      {editing && (
        <RescheduleDialog
          appointment={editing}
          pets={pets}
          vets={vets}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            notify('Cita reprogramada.');
            load();
          }}
        />
      )}

      {cancelTarget && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4"
          onClick={() => setCancelTarget(null)}
          role="dialog"
          aria-modal="true"
        >
          <div className="card w-full max-w-md" onClick={(e) => e.stopPropagation()}>
            <div className="card-header">
              <h3 className="section-title">Cancelar cita</h3>
              <button type="button" className="btn btn-ghost btn-xs" onClick={() => setCancelTarget(null)}>Cerrar</button>
            </div>
            <div className="card-body space-y-4">
              <p className="text-sm text-slate-600">
                ¿Cancelar la cita de <span className="font-medium">{cancelTarget.petName ?? 'paciente'}</span>?
              </p>
              <div className="field">
                <label className="label">Nota (opcional)</label>
                <textarea
                  value={cancelNote}
                  onChange={(e) => setCancelNote(e.target.value)}
                  className="input min-h-20"
                  placeholder="Motivo de la cancelación"
                />
              </div>
              <div className="flex justify-end gap-2">
                <button type="button" className="btn btn-secondary" onClick={() => setCancelTarget(null)}>Volver</button>
                <button type="button" className="btn btn-danger" onClick={confirmCancel}>Cancelar cita</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

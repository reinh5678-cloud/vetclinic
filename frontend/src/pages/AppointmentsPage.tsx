import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../context/AuthContext';
import Modal from '../components/Modal';
import RescheduleDialog from './appointments/RescheduleDialog';
import {
  ACTIVE_STATUSES,
  STATUS_BADGE,
  STATUS_TRANSITIONS,
  TERMINAL_STATUSES,
  URGENCIES,
  buildSlots,
  createAppointment,
  defaultStartLocal,
  fromDateTimeLocal,
  futureDateInput,
  getAppointmentConfig,
  isClosedDay,
  listAppointments,
  parseTimeToMinutes,
  toDateInput,
  updateAppointmentStatus,
  type Appointment,
  type AppointmentFilters,
  type AppointmentStatus,
  type UrgencyLevel,
} from '../api/appointments';
import type { AgendaSettings } from '../api/settings';

interface Pet { id: number; name: string; species: string; }
interface UserLite { id: number; fullName: string; role: string; }

type View = 'today' | 'upcoming' | 'all';
const VIEWS: { id: View; label: string }[] = [
  { id: 'today', label: 'Hoy' },
  { id: 'upcoming', label: 'Próximas' },
  { id: 'all', label: 'Todas' },
];

const PAGE_SIZE = 10;

function pageNumbers(current: number, total: number): (number | '…')[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const pages = new Set<number>([1, total, current, current - 1, current + 1]);
  const sorted = [...pages].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const result: (number | '…')[] = [];
  let prev = 0;
  for (const p of sorted) {
    if (prev && p - prev > 1) result.push('…');
    result.push(p);
    prev = p;
  }
  return result;
}

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
  const [search, setSearch] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [page, setPage] = useState(1);

  const [showCreate, setShowCreate] = useState(false);
  const [petId, setPetId] = useState('');
  const [vetId, setVetId] = useState('');
  const [urgency, setUrgency] = useState('Media');
  const [reason, setReason] = useState('');
  const [startLocal, setStartLocal] = useState(defaultStartLocal());
  const [duration, setDuration] = useState('30');

  const [agenda, setAgenda] = useState<AgendaSettings | null>(null);
  const [date, setDate] = useState(() => toDateInput(new Date()));
  const [slot, setSlot] = useState('');
  const [occupied, setOccupied] = useState<string[]>([]);
  const [agendaTick, setAgendaTick] = useState(0);

  const slots = agenda ? buildSlots(agenda) : [];
  const usingSlots = slots.length > 0;
  const dayClosed = agenda ? isClosedDay(agenda, new Date(`${date}T00:00`)) : false;
  const shownOccupied = agenda && date && vetId && !dayClosed ? occupied : [];

  const [editing, setEditing] = useState<Appointment | null>(null);
  const [statusTarget, setStatusTarget] = useState<Appointment | null>(null);
  const [cancelTarget, setCancelTarget] = useState<Appointment | null>(null);
  const [cancelNote, setCancelNote] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = useCallback(() => {
    if (!canView) return;
    const filters: AppointmentFilters = {};
    if (dateFrom || dateTo) {
      if (dateFrom) filters.from = new Date(`${dateFrom}T00:00`).toISOString();
      if (dateTo) {
        const end = new Date(`${dateTo}T00:00`);
        end.setDate(end.getDate() + 1);
        filters.to = end.toISOString();
      }
    } else if (view === 'today') {
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
  }, [canView, view, filterVet, filterStatus, dateFrom, dateTo]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => {
    if (!canView) return;
    api<Pet[]>('/patients/pets')
      .then(setPets)
      .catch(console.error);
    api<UserLite[]>('/auth/users')
      .then((users) => setVets(users.filter((u) => u.role === 'Veterinario')))
      .catch(console.error);
    getAppointmentConfig()
      .then((cfg) => { setAgenda(cfg); setDuration(String(cfg.defaultDurationMinutes)); })
      .catch(console.error);
  }, [canView]);

  useEffect(() => {
    if (!agenda || !date || !vetId || dayClosed) return;
    const rangeStart = new Date(`${date}T00:00`);
    const rangeEnd = new Date(rangeStart);
    rangeEnd.setDate(rangeEnd.getDate() + 1);
    const dur = Number(duration) || agenda.defaultDurationMinutes;
    const generated = buildSlots(agenda);
    listAppointments({ from: rangeStart.toISOString(), to: rangeEnd.toISOString(), vetId: Number(vetId) })
      .then((appts) => {
        const taken = generated.filter((s) => {
          const t = parseTimeToMinutes(s);
          return appts.some((a) => {
            if (!ACTIVE_STATUSES.includes(a.status)) return false;
            const d = new Date(a.startTime);
            const am = d.getHours() * 60 + d.getMinutes();
            return am < t + dur && am + a.durationMinutes > t;
          });
        });
        setOccupied(taken);
      })
      .catch(() => setOccupied([]));
  }, [agenda, date, vetId, duration, dayClosed, agendaTick]);

  const term = search.trim().toLowerCase();
  const filtered = term
    ? appointments.filter((a) => {
        const hay = [a.petName, a.vetName, a.reason, a.notes, a.urgency, a.status, new Date(a.startTime).toLocaleString()]
          .filter(Boolean)
          .join(' ')
          .toLowerCase();
        return hay.includes(term);
      })
    : appointments;
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageStart = (currentPage - 1) * PAGE_SIZE;
  const paged = filtered.slice(pageStart, pageStart + PAGE_SIZE);

  useEffect(() => { setPage(1); }, [search, view, filterVet, filterStatus, dateFrom, dateTo]);

  const notify = (text: string) => { setErr(''); setMsg(text); };

  const clearFilters = () => {
    setSearch('');
    setDateFrom('');
    setDateTo('');
    setFilterVet('');
    setFilterStatus('');
  };

  const openCreate = () => {
    setErr('');
    setMsg('');
    setShowCreate(true);
  };

  const create = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !vetId) return;
    if (usingSlots ? !slot : !startLocal) return;
    setErr('');
    try {
      await createAppointment({
        startTime: usingSlots ? fromDateTimeLocal(`${date}T${slot}`) : fromDateTimeLocal(startLocal),
        durationMinutes: Number(duration),
        urgency: urgency as UrgencyLevel,
        reason,
        petId: Number(petId),
        vetId: Number(vetId),
      });
      setReason('');
      if (usingSlots) {
        setSlot('');
        setAgendaTick((t) => t + 1);
      } else {
        setStartLocal(defaultStartLocal());
      }
      setShowCreate(false);
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
        <div className="flex flex-wrap items-center gap-2">
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
          {canManage && (
            <button type="button" className="btn btn-primary" onClick={openCreate}>Nueva cita</button>
          )}
        </div>
      </div>

      <div className="card card-pad flex flex-wrap items-end gap-3">
        <div className="field min-w-56 flex-1">
          <label className="label">Buscar</label>
          <input
            type="search"
            placeholder="Paciente, veterinario, motivo, estado…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="input"
          />
        </div>
        <div className="field">
          <label className="label">Desde</label>
          <input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} className="input" />
        </div>
        <div className="field">
          <label className="label">Hasta</label>
          <input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} className="input" />
        </div>
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
        {(search || dateFrom || dateTo || filterVet || filterStatus) && (
          <button type="button" className="btn btn-secondary" onClick={clearFilters}>Limpiar</button>
        )}
        <div className="flex-1" />
        <div className="text-sm text-slate-500">
          {filtered.length} de {appointments.length} {appointments.length === 1 ? 'cita' : 'citas'}
        </div>
      </div>

      {err && !showCreate && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

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
            {paged.map((a) => {
              const terminal = TERMINAL_STATUSES.includes(a.status);
              return (
                <tr key={a.id}>
                  <td className="whitespace-nowrap">
                    <div>{new Date(a.startTime).toLocaleString()}</div>
                    <div className="text-xs text-slate-400">{a.durationMinutes} min</div>
                  </td>
                  <td className="text-center">{a.petName ?? '-'}</td>
                  <td className="text-center">{a.vetName ?? '-'}</td>
                  <td className="text-center">{a.urgency}</td>
                  <td className="group max-w-xs">
                    <div className="cell-expand">
                      <div>{a.reason}</div>
                      {a.notes && <div className="text-xs text-slate-400">{a.notes}</div>}
                    </div>
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
                        <button
                          type="button"
                          className="btn btn-secondary btn-xs"
                          disabled={terminal}
                          onClick={() => setStatusTarget(a)}
                        >
                          Estado
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              );
            })}
            {filtered.length === 0 && (
              <tr>
                <td colSpan={canManage ? 7 : 6} className="py-10 text-center text-slate-400">
                  No hay citas para este filtro.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {filtered.length > 0 && (
        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-xs text-slate-500">
            Mostrando {pageStart + 1}–{Math.min(pageStart + PAGE_SIZE, filtered.length)} de {filtered.length}
          </p>
          {totalPages > 1 && (
            <div className="flex items-center gap-1">
              <button
                type="button"
                className="btn btn-secondary btn-xs"
                disabled={currentPage === 1}
                onClick={() => setPage(currentPage - 1)}
              >
                Anterior
              </button>
              {pageNumbers(currentPage, totalPages).map((p, i) =>
                p === '…' ? (
                  <span key={`gap-${i}`} className="px-1 text-xs text-slate-400">…</span>
                ) : (
                  <button
                    key={p}
                    type="button"
                    className={`btn btn-xs ${p === currentPage ? 'btn-primary' : 'btn-secondary'}`}
                    onClick={() => setPage(p)}
                  >
                    {p}
                  </button>
                ),
              )}
              <button
                type="button"
                className="btn btn-secondary btn-xs"
                disabled={currentPage === totalPages}
                onClick={() => setPage(currentPage + 1)}
              >
                Siguiente
              </button>
            </div>
          )}
        </div>
      )}

      <div className="alert alert-info">
        <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>Los cambios de estado se validan contra las transiciones permitidas; el cruce de horarios (overlap) se valida en el backend al agendar o reprogramar.</p>
      </div>

      {showCreate && (
        <Modal title="Nueva cita" onClose={() => setShowCreate(false)} wide>
          <form onSubmit={create} className="space-y-3">
            {err && <div className="alert alert-error">{err}</div>}
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="field">
                <label className="label">Paciente</label>
                <select value={petId} onChange={(e) => setPetId(e.target.value)} className="select" required>
                  <option value="">-- Paciente --</option>
                  {pets.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.species})</option>)}
                </select>
              </div>
              <div className="field">
                <label className="label">Veterinario</label>
                <select value={vetId} onChange={(e) => setVetId(e.target.value)} className="select" required>
                  <option value="">-- Veterinario --</option>
                  {vets.map((v) => <option key={v.id} value={v.id}>{v.fullName}</option>)}
                </select>
              </div>
              {usingSlots ? (
                <div className="field">
                  <label className="label">Fecha</label>
                  <input
                    type="date"
                    value={date}
                    min={toDateInput(new Date())}
                    max={agenda ? futureDateInput(agenda.maxAdvanceDays) : undefined}
                    onChange={(e) => { setDate(e.target.value); setSlot(''); }}
                    className="input"
                    required
                  />
                </div>
              ) : (
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
              )}
              <div className="field">
                <label className="label">Duración (min)</label>
                <input
                  type="number"
                  min={agenda ? agenda.slotMinutes : 5}
                  max={480}
                  step={5}
                  value={duration}
                  onChange={(e) => setDuration(e.target.value)}
                  className="input"
                  required
                />
              </div>
              <div className="field">
                <label className="label">Urgencia</label>
                <select value={urgency} onChange={(e) => setUrgency(e.target.value)} className="select">
                  {URGENCIES.map((u) => <option key={u}>{u}</option>)}
                </select>
              </div>
              <div className="field sm:col-span-2">
                <label className="label">Motivo</label>
                <input
                  placeholder="Motivo de la cita (ej: control, vacunación)"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  className="input"
                  required
                />
              </div>
            </div>
            {usingSlots && (
              <div className="field">
                <label className="label">Franja horaria</label>
                {dayClosed ? (
                  <p className="text-sm text-amber-600">La clínica no atiende este día.</p>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    {slots.map((s) => {
                      const isTaken = shownOccupied.includes(s);
                      return (
                        <button
                          key={s}
                          type="button"
                          disabled={isTaken}
                          onClick={() => setSlot(s)}
                          className={
                            (slot === s ? 'btn btn-primary btn-xs' : 'btn btn-secondary btn-xs') +
                            ' disabled:cursor-not-allowed disabled:opacity-40'
                          }
                          title={isTaken ? 'Franja ocupada' : `Agendar a las ${s}`}
                        >
                          {s}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn btn-secondary" onClick={() => setShowCreate(false)}>Cancelar</button>
              <button type="submit" className="btn btn-primary">Agendar</button>
            </div>
          </form>
        </Modal>
      )}

      {statusTarget && (
        <Modal title={`Estado · ${statusTarget.petName ?? 'cita'}`} onClose={() => setStatusTarget(null)}>
          <div className="space-y-3">
            <p className="text-sm text-slate-500">
              Estado actual:{' '}
              <span className={`badge ${STATUS_BADGE[statusTarget.status] ?? 'badge-neutral'}`}>{statusTarget.status}</span>
            </p>
            {(STATUS_TRANSITIONS[statusTarget.status]?.length ?? 0) > 0 ? (
              <div className="flex flex-col gap-2">
                {STATUS_TRANSITIONS[statusTarget.status].map((s) => (
                  <button
                    key={s}
                    type="button"
                    className="btn btn-secondary justify-start"
                    onClick={() => {
                      const a = statusTarget;
                      setStatusTarget(null);
                      changeStatus(a, s);
                    }}
                  >
                    {s}
                  </button>
                ))}
              </div>
            ) : (
              <p className="text-sm text-slate-400">No hay transiciones disponibles desde este estado.</p>
            )}
          </div>
        </Modal>
      )}

      {editing && (
        <RescheduleDialog
          appointment={editing}
          pets={pets}
          vets={vets}
          agenda={agenda}
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

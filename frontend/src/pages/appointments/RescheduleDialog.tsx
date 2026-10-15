import { useEffect, useState, type FormEvent } from 'react';
import {
  ACTIVE_STATUSES,
  URGENCIES,
  buildSlots,
  fromDateTimeLocal,
  futureDateInput,
  isClosedDay,
  listAppointments,
  parseTimeToMinutes,
  toDateInput,
  toDateTimeLocal,
  updateAppointment,
  type Appointment,
  type UrgencyLevel,
} from '../../api/appointments';
import type { AgendaSettings } from '../../api/settings';

interface PetOption { id: number; name: string; species: string; }
interface VetOption { id: number; fullName: string; }

interface Props {
  appointment: Appointment;
  pets: PetOption[];
  vets: VetOption[];
  agenda: AgendaSettings | null;
  onClose: () => void;
  onSaved: (appointment: Appointment) => void;
}

export default function RescheduleDialog({ appointment, pets, vets, agenda, onClose, onSaved }: Props) {
  const [startLocal, setStartLocal] = useState(() => toDateTimeLocal(appointment.startTime));
  const [duration, setDuration] = useState(String(appointment.durationMinutes));
  const [urgency, setUrgency] = useState(appointment.urgency);
  const [reason, setReason] = useState(appointment.reason);
  const [petId, setPetId] = useState(appointment.petId ? String(appointment.petId) : '');
  const [vetId, setVetId] = useState(appointment.vetId ? String(appointment.vetId) : '');
  const [date, setDate] = useState(() => toDateInput(new Date(appointment.startTime)));
  const [slot, setSlot] = useState(() => {
    if (!agenda) return '';
    const d = new Date(appointment.startTime);
    const hhmm = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
    return buildSlots(agenda).includes(hhmm) ? hhmm : '';
  });
  const [occupied, setOccupied] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState('');

  const slots = agenda ? buildSlots(agenda) : [];
  const usingSlots = slots.length > 0;
  const dayClosed = agenda ? isClosedDay(agenda, new Date(`${date}T00:00`)) : false;
  const shownOccupied = agenda && date && vetId && !dayClosed ? occupied : [];

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

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
            if (a.id === appointment.id || !ACTIVE_STATUSES.includes(a.status)) return false;
            const d = new Date(a.startTime);
            const am = d.getHours() * 60 + d.getMinutes();
            return am < t + dur && am + a.durationMinutes > t;
          });
        });
        setOccupied(taken);
      })
      .catch(() => setOccupied([]));
  }, [agenda, date, vetId, duration, dayClosed, appointment.id]);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !vetId) return;
    if (usingSlots ? !slot : !startLocal) return;
    setErr('');
    setSaving(true);
    try {
      const updated = await updateAppointment(appointment.id, {
        startTime: usingSlots ? fromDateTimeLocal(`${date}T${slot}`) : fromDateTimeLocal(startLocal),
        durationMinutes: Number(duration),
        urgency: urgency as UrgencyLevel,
        reason,
        petId: Number(petId),
        vetId: Number(vetId),
      });
      onSaved(updated);
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
    >
      <div className="card w-full max-w-lg" onClick={(e) => e.stopPropagation()}>
        <div className="card-header">
          <h3 className="section-title">Reprogramar cita</h3>
          <button type="button" className="btn btn-ghost btn-xs" onClick={onClose}>Cerrar</button>
        </div>
        <form className="card-body space-y-4" onSubmit={submit}>
          {err && <div className="alert alert-error">{err}</div>}

          {usingSlots ? (
            <div className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
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
              </div>
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
                          title={isTaken ? 'Franja ocupada' : `Reprogramar a las ${s}`}
                        >
                          {s}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
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
            </div>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
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
          </div>

          <div className="field">
            <label className="label">Urgencia</label>
            <select value={urgency} onChange={(e) => setUrgency(e.target.value)} className="select">
              {URGENCIES.map((u) => <option key={u}>{u}</option>)}
            </select>
          </div>

          <div className="field">
            <label className="label">Motivo</label>
            <input value={reason} onChange={(e) => setReason(e.target.value)} className="input" required />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={saving}>Cancelar</button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Guardando…' : 'Guardar cambios'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

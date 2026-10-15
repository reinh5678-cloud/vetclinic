import { useEffect, useState, type FormEvent } from 'react';
import {
  URGENCIES,
  fromDateTimeLocal,
  toDateTimeLocal,
  updateAppointment,
  type Appointment,
  type UrgencyLevel,
} from '../../api/appointments';

interface PetOption { id: number; name: string; species: string; }
interface VetOption { id: number; fullName: string; }

interface Props {
  appointment: Appointment;
  pets: PetOption[];
  vets: VetOption[];
  onClose: () => void;
  onSaved: (appointment: Appointment) => void;
}

export default function RescheduleDialog({ appointment, pets, vets, onClose, onSaved }: Props) {
  const [startLocal, setStartLocal] = useState(() => toDateTimeLocal(appointment.startTime));
  const [duration, setDuration] = useState(String(appointment.durationMinutes));
  const [urgency, setUrgency] = useState(appointment.urgency);
  const [reason, setReason] = useState(appointment.reason);
  const [petId, setPetId] = useState(appointment.petId ? String(appointment.petId) : '');
  const [vetId, setVetId] = useState(appointment.vetId ? String(appointment.vetId) : '');
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState('');

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !vetId || !startLocal) return;
    setErr('');
    setSaving(true);
    try {
      const updated = await updateAppointment(appointment.id, {
        startTime: fromDateTimeLocal(startLocal),
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
                min={5}
                max={480}
                step={5}
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
                className="input"
                required
              />
            </div>
          </div>

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

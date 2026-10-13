import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';

interface Pet { id: number; name: string; species: string; }
interface Evolution { id: number; recordedAt: string; temperature: number | null; heartRate: number | null; respiratoryRate: number | null; observations: string; }
interface Medication { id: number; medicationName: string; dose: string; route: string; administeredAt: string; }
interface Hospitalization {
  id: number; startDate: string; endDate: string | null; status: string; reason: string;
  petId: number; petName: string | null;
}
interface HospitalizationDetail extends Hospitalization {
  notes: string;
  evolutions: Evolution[];
  medications: Medication[];
}

export default function HospitalizationsPage() {
  const [list, setList] = useState<Hospitalization[]>([]);
  const [pets, setPets] = useState<Pet[]>([]);
  const [petId, setPetId] = useState<number | null>(null);
  const [reason, setReason] = useState('');
  const [notes, setNotes] = useState('');

  const load = () => api<Hospitalization[]>('/hospitalizations/active').then(setList).catch(console.error);
  useEffect(() => { load(); api<Pet[]>('/patients/pets').then(setPets).catch(console.error); }, []);

  const start = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId) return;
    await api('/hospitalizations', { method: 'POST', body: JSON.stringify({ petId, assignedVetId: null, reason, notes }) });
    setReason(''); setNotes(''); setPetId(null);
    load();
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Hospitalización</h2>
          <p className="page-subtitle">Ingreso, evolución y medicación de pacientes internados.</p>
        </div>
      </div>

      <form onSubmit={start} className="card card-pad space-y-4">
        <h3 className="section-title">Ingresar paciente</h3>
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="field">
            <label className="label">Paciente</label>
            <select value={petId ?? ''} onChange={(e) => setPetId(e.target.value ? Number(e.target.value) : null)} className="select">
              <option value="">-- Paciente --</option>
              {pets.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.species})</option>)}
            </select>
          </div>
          <div className="field">
            <label className="label">Motivo de hospitalización</label>
            <input placeholder="Motivo de hospitalización" value={reason} onChange={(e) => setReason(e.target.value)} className="input" />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Notas iniciales</label>
            <input placeholder="Notas iniciales" value={notes} onChange={(e) => setNotes(e.target.value)} className="input" />
          </div>
        </div>
        <button type="submit" className="btn btn-primary">Ingresar</button>
      </form>

      {list.length === 0 && (
        <div className="card card-pad text-center text-sm text-slate-400">
          No hay pacientes hospitalizados activos.
        </div>
      )}

      {list.map((h) => (
        <HospitalizationCard key={h.id} h={h} onChanged={load} />
      ))}
    </div>
  );
}

function HospitalizationCard({ h, onChanged }: { h: Hospitalization; onChanged: () => void }) {
  const [evoOpen, setEvoOpen] = useState(false);
  const [medOpen, setMedOpen] = useState(false);
  const [detail, setDetail] = useState<HospitalizationDetail>({
    ...h, notes: '', evolutions: [], medications: [],
  });

  const loadDetail = async () => {
    const d = await api<HospitalizationDetail>(`/hospitalizations/${h.id}`);
    setDetail(d);
  };

  const reload = async () => {
    await loadDetail();
    onChanged();
  };

  useEffect(() => {
    api<HospitalizationDetail>(`/hospitalizations/${h.id}`).then(setDetail).catch(console.error);
  }, [h.id]);

  const addEvolution = async (e: FormEvent) => {
    e.preventDefault();
    const data = new FormData(e.target as HTMLFormElement);
    await api(`/hospitalizations/${h.id}/evolutions`, { method: 'POST', body: JSON.stringify({
      temperature: data.get('temperature') ? Number(data.get('temperature')) : null,
      heartRate: data.get('heartRate') ? Number(data.get('heartRate')) : null,
      respiratoryRate: data.get('respiratoryRate') ? Number(data.get('respiratoryRate')) : null,
      observations: data.get('observations') || '' }) });
    (e.target as HTMLFormElement).reset();
    setEvoOpen(false);
    reload();
  };

  const addMedication = async (e: FormEvent) => {
    e.preventDefault();
    const data = new FormData(e.target as HTMLFormElement);
    await api(`/hospitalizations/${h.id}/medications`, { method: 'POST', body: JSON.stringify({
      medicationName: data.get('medicationName'), dose: data.get('dose'),
      route: data.get('route'), administeredAt: new Date().toISOString() }) });
    (e.target as HTMLFormElement).reset();
    setMedOpen(false);
    reload();
  };

  const discharge = async () => {
    if (!confirm('¿Dar de alta a este paciente?')) return;
    await api(`/hospitalizations/${h.id}/discharge`, { method: 'POST', body: '{}' });
    reload();
  };

  return (
    <div className="card overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-rose-100 bg-rose-50 px-5 py-3">
        <div className="flex items-center gap-3">
          <span className="flex h-8 w-8 items-center justify-center rounded-full bg-rose-100 text-rose-600">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M21 8.25c0-2.485-2.099-4.5-4.688-4.5-1.935 0-3.597 1.126-4.312 2.733-.715-1.607-2.377-2.733-4.313-2.733C5.1 3.75 3 5.765 3 8.25c0 7.22 9 12 9 12s9-4.78 9-12z" />
            </svg>
          </span>
          <strong className="text-slate-800">{detail.petName ?? `Mascota #${detail.petId}`}</strong>
          <span className="badge badge-danger">{detail.status}</span>
        </div>
        <span className="text-xs text-slate-500">{new Date(detail.startDate).toLocaleString()}</span>
      </div>

      <div className="space-y-3 p-5">
        <p className="text-sm"><strong className="text-slate-700">Motivo:</strong> <span className="text-slate-600">{detail.reason || '—'}</span></p>
        {detail.notes && <p className="text-sm"><strong className="text-slate-700">Notas:</strong> <span className="text-slate-600">{detail.notes}</span></p>}

        <div className="flex flex-wrap gap-2 pt-1">
          <button className="btn btn-secondary btn-sm" onClick={() => setEvoOpen(!evoOpen)}>{evoOpen ? 'Cancelar' : '+ Evolución'}</button>
          <button className="btn btn-secondary btn-sm" onClick={() => setMedOpen(!medOpen)}>{medOpen ? 'Cancelar' : '+ Medicación'}</button>
          <button className="btn btn-success btn-sm" onClick={discharge}>Dar de alta</button>
        </div>

        {evoOpen && (
          <form onSubmit={addEvolution} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
            <div className="grid gap-2 sm:grid-cols-3">
              <input name="temperature" type="number" step="0.1" placeholder="T° (°C)" className="input" />
              <input name="heartRate" type="number" placeholder="FC (/min)" className="input" />
              <input name="respiratoryRate" type="number" placeholder="FR (/min)" className="input" />
            </div>
            <input name="observations" placeholder="Observaciones" className="input" />
            <button type="submit" className="btn btn-primary btn-sm">Guardar evolución</button>
          </form>
        )}

        {medOpen && (
          <form onSubmit={addMedication} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
            <input name="medicationName" placeholder="Medicamento *" className="input" required />
            <input name="dose" placeholder="Dosis (ej: 1ml)" className="input" />
            <input name="route" placeholder="Vía (ej: IV, oral)" className="input" />
            <button type="submit" className="btn btn-primary btn-sm">Administrar</button>
          </form>
        )}

        <div className="grid gap-5 pt-1 lg:grid-cols-2">
          <section>
            <h4 className="mb-2 text-sm font-semibold text-slate-700">Evolución ({detail.evolutions.length})</h4>
            <div className="space-y-2">
              {detail.evolutions.map((ev) => (
                <div key={ev.id} className="rounded-lg border border-slate-200 bg-slate-50 p-3">
                  <strong className="text-xs text-slate-700">{new Date(ev.recordedAt).toLocaleString()}</strong>
                  <div className="text-[13px] text-slate-600">
                    T° {ev.temperature ?? '—'}°C · FC {ev.heartRate ?? '—'} · FR {ev.respiratoryRate ?? '—'}
                  </div>
                  {ev.observations && <div className="text-[13px] text-slate-500">{ev.observations}</div>}
                </div>
              ))}
              {detail.evolutions.length === 0 && <p className="text-sm text-slate-400">Sin registros de evolución.</p>}
            </div>
          </section>
          <section>
            <h4 className="mb-2 text-sm font-semibold text-slate-700">Medicación administrada ({detail.medications.length})</h4>
            <div className="space-y-2">
              {detail.medications.map((m) => (
                <div key={m.id} className="rounded-lg border border-slate-200 bg-slate-50 p-3">
                  <strong className="text-sm text-slate-700">{m.medicationName}</strong>{' '}
                  <span className="text-sm text-slate-600">{m.dose && `· ${m.dose}`}{m.route && ` (${m.route})`}</span>
                  <div className="text-[13px] text-slate-500">{new Date(m.administeredAt).toLocaleString()}</div>
                </div>
              ))}
              {detail.medications.length === 0 && <p className="text-sm text-slate-400">Sin medicación registrada.</p>}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}

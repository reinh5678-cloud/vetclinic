import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { useAuth } from '../context/AuthContext';
import ConfirmDialog from '../components/ConfirmDialog';
import {
  STATUSES,
  STATUS_BADGE,
  STATUS_LABEL,
  STATUS_TRANSITIONS,
  addEvolution,
  addMedication,
  changeStatus,
  discharge,
  getDetail,
  isTerminal,
  listActive,
  listHospitalizations,
  startHospitalization,
  type Hospitalization,
  type HospitalizationDetail,
  type HospitalizationStatus,
} from '../api/hospitalizations';

interface Pet { id: number; name: string; species: string; }
interface UserLite { id: number; fullName: string; role: string; }

type Tab = 'active' | 'all';

const TRANSITION_LABEL: Record<HospitalizationStatus, string> = {
  Ingresado: 'Ingresar',
  EnTratamiento: 'Iniciar tratamiento',
  EnRecuperacion: 'Pasar a recuperación',
  Alta: 'Dar de alta',
  Fallecido: 'Registrar fallecimiento',
};

const emptyForm = { petId: '', vetId: '', reason: '', notes: '' };

export default function HospitalizationsPage() {
  const { can } = useAuth();
  const canManage = can('hospitalization.manage');

  const [tab, setTab] = useState<Tab>('active');
  const [active, setActive] = useState<Hospitalization[]>([]);
  const [all, setAll] = useState<Hospitalization[]>([]);
  const [pets, setPets] = useState<Pet[]>([]);
  const [vets, setVets] = useState<UserLite[]>([]);

  const [statusFilter, setStatusFilter] = useState('');
  const [petFilter, setPetFilter] = useState('');

  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const notify = useCallback((text: string) => { setErr(''); setMsg(text); }, []);
  const reportError = useCallback((text: string) => { setMsg(''); setErr(text); }, []);

  const loadActive = useCallback(() => {
    listActive().then(setActive).catch((e) => reportError((e as Error).message));
  }, [reportError]);

  const loadAll = useCallback(() => {
    listHospitalizations({
      status: statusFilter || undefined,
      petId: petFilter ? Number(petFilter) : undefined,
    }).then(setAll).catch((e) => reportError((e as Error).message));
  }, [statusFilter, petFilter, reportError]);

  useEffect(() => {
    api<Pet[]>('/patients/pets').then(setPets).catch(console.error);
    api<UserLite[]>('/auth/users')
      .then((users) => setVets(users.filter((u) => u.role === 'Veterinario')))
      .catch(console.error);
  }, []);

  useEffect(() => { loadActive(); }, [loadActive]);
  useEffect(() => { if (tab === 'all') loadAll(); }, [tab, loadAll]);

  const reload = useCallback(() => {
    loadActive();
    loadAll();
  }, [loadActive, loadAll]);

  const start = async (e: FormEvent) => {
    e.preventDefault();
    if (!form.petId) return;
    setErr('');
    setSaving(true);
    try {
      await startHospitalization({
        petId: Number(form.petId),
        assignedVetId: form.vetId ? Number(form.vetId) : null,
        reason: form.reason,
        notes: form.notes,
      });
      setForm(emptyForm);
      notify('Paciente hospitalizado.');
      loadActive();
      if (tab === 'all') loadAll();
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const rows = tab === 'active' ? active : all;

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Hospitalización</h2>
          <p className="page-subtitle">Ingreso, evolución y medicación de pacientes internados.</p>
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            className={`btn btn-sm ${tab === 'active' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setTab('active')}
          >
            Activos
          </button>
          <button
            type="button"
            className={`btn btn-sm ${tab === 'all' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setTab('all')}
          >
            Todas
          </button>
        </div>
      </div>

      {msg && <div className="alert alert-success">{msg}</div>}
      {err && <div className="alert alert-error">{err}</div>}

      {canManage && tab === 'active' && (
        <form onSubmit={start} className="card card-pad space-y-4">
          <h3 className="section-title">Ingresar paciente</h3>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="field">
              <label className="label">Paciente</label>
              <select value={form.petId} onChange={(e) => setForm({ ...form, petId: e.target.value })} className="select" required>
                <option value="">-- Paciente --</option>
                {pets.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.species})</option>)}
              </select>
            </div>
            <div className="field">
              <label className="label">Veterinario (opcional)</label>
              <select value={form.vetId} onChange={(e) => setForm({ ...form, vetId: e.target.value })} className="select">
                <option value="">-- Sin asignar --</option>
                {vets.map((v) => <option key={v.id} value={v.id}>{v.fullName}</option>)}
              </select>
            </div>
            <div className="field sm:col-span-2">
              <label className="label">Motivo de hospitalización</label>
              <input placeholder="Motivo de hospitalización" value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} className="input" required />
            </div>
            <div className="field sm:col-span-2">
              <label className="label">Notas iniciales</label>
              <input placeholder="Notas iniciales" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} className="input" />
            </div>
          </div>
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Ingresando…' : 'Ingresar'}</button>
        </form>
      )}

      {tab === 'all' && (
        <div className="card card-pad">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="field">
              <label className="label">Estado</label>
              <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="select">
                <option value="">Todos</option>
                {STATUSES.map((s) => <option key={s} value={s}>{STATUS_LABEL[s]}</option>)}
              </select>
            </div>
            <div className="field">
              <label className="label">Paciente</label>
              <select value={petFilter} onChange={(e) => setPetFilter(e.target.value)} className="select">
                <option value="">Todos</option>
                {pets.map((p) => <option key={p.id} value={p.id}>{p.name} ({p.species})</option>)}
              </select>
            </div>
          </div>
        </div>
      )}

      {rows.length === 0 && (
        <div className="card card-pad text-center text-sm text-slate-400">
          {tab === 'active' ? 'No hay pacientes hospitalizados activos.' : 'No hay hospitalizaciones que coincidan con el filtro.'}
        </div>
      )}

      {rows.map((h) => (
        <HospitalizationCard
          key={h.id}
          h={h}
          canManage={canManage}
          onChanged={reload}
          onNotify={notify}
          onError={reportError}
        />
      ))}
    </div>
  );
}

interface CardProps {
  h: Hospitalization;
  canManage: boolean;
  onChanged: () => void;
  onNotify: (text: string) => void;
  onError: (text: string) => void;
}

function HospitalizationCard({ h, canManage, onChanged, onNotify, onError }: CardProps) {
  const [detail, setDetail] = useState<HospitalizationDetail | null>(null);
  const [evoOpen, setEvoOpen] = useState(false);
  const [medOpen, setMedOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [confirming, setConfirming] = useState<HospitalizationStatus | null>(null);

  const loadDetail = useCallback(() => {
    getDetail(h.id).then(setDetail).catch((e) => onError((e as Error).message));
  }, [h.id, onError]);

  useEffect(() => { loadDetail(); }, [loadDetail]);

  const terminal = isTerminal(h.status);

  const run = async (action: () => Promise<unknown>, successText: string) => {
    setBusy(true);
    try {
      await action();
      onNotify(successText);
      onChanged();
    } catch (ex) {
      onError((ex as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const submitEvolution = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formEl = e.currentTarget;
    const data = new FormData(formEl);
    await run(() => addEvolution(h.id, {
      temperature: data.get('temperature') ? Number(data.get('temperature')) : null,
      heartRate: data.get('heartRate') ? Number(data.get('heartRate')) : null,
      respiratoryRate: data.get('respiratoryRate') ? Number(data.get('respiratoryRate')) : null,
      observations: (data.get('observations') as string) || '',
    }), 'Evolución registrada.');
    formEl.reset();
    setEvoOpen(false);
    loadDetail();
  };

  const submitMedication = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formEl = e.currentTarget;
    const data = new FormData(formEl);
    const at = data.get('administeredAt') as string | null;
    await run(() => addMedication(h.id, {
      medicationName: (data.get('medicationName') as string) || '',
      dose: (data.get('dose') as string) || '',
      route: (data.get('route') as string) || '',
      administeredAt: at ? new Date(at).toISOString() : new Date().toISOString(),
    }), 'Medicación administrada.');
    formEl.reset();
    setMedOpen(false);
    loadDetail();
  };

  const applyTransition = (target: HospitalizationStatus) => {
    if (isTerminal(target)) {
      setConfirming(target);
      return;
    }
    run(() => changeStatus(h.id, target), `Estado actualizado: ${STATUS_LABEL[target]}.`);
  };

  const confirmTerminal = () => {
    const target = confirming;
    if (!target) return;
    setConfirming(null);
    if (target === 'Alta') run(() => discharge(h.id), 'Paciente dado de alta.');
    else run(() => changeStatus(h.id, target), 'Fallecimiento registrado.');
  };

  const d = detail ?? { ...h, notes: '', evolutions: [], medications: [] };

  return (
    <div className="card overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-rose-100 bg-rose-50 px-5 py-3">
        <div className="flex flex-wrap items-center gap-3">
          <span className="flex h-8 w-8 items-center justify-center rounded-full bg-rose-100 text-rose-600">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M21 8.25c0-2.485-2.099-4.5-4.688-4.5-1.935 0-3.597 1.126-4.312 2.733-.715-1.607-2.377-2.733-4.313-2.733C5.1 3.75 3 5.765 3 8.25c0 7.22 9 12 9 12s9-4.78 9-12z" />
            </svg>
          </span>
          <strong className="text-slate-800">{d.petName ?? `Mascota #${d.petId}`}</strong>
          <span className={`badge ${STATUS_BADGE[h.status]}`}>{STATUS_LABEL[h.status]}</span>
          {d.assignedVetName && <span className="text-xs text-slate-500">Vet: {d.assignedVetName}</span>}
        </div>
        <span className="text-xs text-slate-500">
          {new Date(d.startDate).toLocaleString()} · {duration(d.startDate, d.endDate)}
        </span>
      </div>

      <div className="space-y-3 p-5">
        <p className="text-sm"><strong className="text-slate-700">Motivo:</strong> <span className="text-slate-600">{d.reason || '—'}</span></p>
        {d.notes && <p className="text-sm"><strong className="text-slate-700">Notas:</strong> <span className="text-slate-600">{d.notes}</span></p>}

        {canManage && !terminal && (
          <div className="flex flex-wrap gap-2 pt-1">
            {STATUS_TRANSITIONS[h.status].map((target) => (
              <button
                key={target}
                className={`btn btn-sm ${isTerminal(target) ? 'btn-danger' : 'btn-secondary'}`}
                disabled={busy}
                onClick={() => applyTransition(target)}
              >
                {TRANSITION_LABEL[target]}
              </button>
            ))}
            <button className="btn btn-secondary btn-sm" disabled={busy} onClick={() => setEvoOpen(!evoOpen)}>{evoOpen ? 'Cancelar' : '+ Evolución'}</button>
            <button className="btn btn-secondary btn-sm" disabled={busy} onClick={() => setMedOpen(!medOpen)}>{medOpen ? 'Cancelar' : '+ Medicación'}</button>
          </div>
        )}

        {evoOpen && !terminal && (
          <form onSubmit={submitEvolution} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
            <div className="grid gap-3 sm:grid-cols-3">
              <input name="temperature" type="number" step="0.1" min={0} max={60} placeholder="Temperatura (°C)" className="input" />
              <input name="heartRate" type="number" min={0} max={400} placeholder="FC (lpm)" className="input" />
              <input name="respiratoryRate" type="number" min={0} max={200} placeholder="FR (rpm)" className="input" />
            </div>
            <input name="observations" placeholder="Observaciones" className="input" />
            <button type="submit" className="btn btn-primary btn-sm" disabled={busy}>Guardar</button>
          </form>
        )}

        {medOpen && !terminal && (
          <form onSubmit={submitMedication} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <input name="medicationName" placeholder="Medicamento *" className="input" required />
              <input name="dose" placeholder="Dosis (ej: 1ml)" className="input" />
              <input name="route" placeholder="Vía (ej: IV, oral)" className="input" />
              <input name="administeredAt" type="datetime-local" defaultValue={nowLocal()} className="input" />
            </div>
            <button type="submit" className="btn btn-primary btn-sm" disabled={busy}>Administrar</button>
          </form>
        )}

        <div className="grid gap-5 pt-1 lg:grid-cols-2">
          <section>
            <h4 className="mb-2 text-sm font-semibold text-slate-700">Evolución ({(detail?.evolutions ?? []).length})</h4>
            <div className="space-y-2">
              {(detail?.evolutions ?? []).map((ev) => (
                <div key={ev.id} className="rounded-lg border border-slate-200 bg-slate-50 p-3">
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-slate-500">{new Date(ev.recordedAt).toLocaleString()}</span>
                    {ev.recordedByName && <span className="text-xs text-slate-400">{ev.recordedByName}</span>}
                  </div>
                  <p className="mt-1 text-sm text-slate-600">
                    {ev.temperature != null && <>T° {ev.temperature} °C </>}
                    {ev.heartRate != null && <>· FC {ev.heartRate} </>}
                    {ev.respiratoryRate != null && <>· FR {ev.respiratoryRate}</>}
                  </p>
                  {ev.observations && <p className="text-sm text-slate-600">{ev.observations}</p>}
                </div>
              ))}
              {(detail?.evolutions ?? []).length === 0 && <p className="text-sm text-slate-400">Sin evoluciones registradas.</p>}
            </div>
          </section>

          <section>
            <h4 className="mb-2 text-sm font-semibold text-slate-700">Medicación ({(detail?.medications ?? []).length})</h4>
            <div className="space-y-2">
              {(detail?.medications ?? []).map((m) => (
                <div key={m.id} className="rounded-lg border border-slate-200 bg-slate-50 p-3">
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-slate-500">{new Date(m.administeredAt).toLocaleString()}</span>
                    {m.administeredByName && <span className="text-xs text-slate-400">{m.administeredByName}</span>}
                  </div>
                  <p className="mt-1 text-sm text-slate-700"><strong>{m.medicationName}</strong>{m.dose && ` · ${m.dose}`}{m.route && ` · ${m.route}`}</p>
                </div>
              ))}
              {(detail?.medications ?? []).length === 0 && <p className="text-sm text-slate-400">Sin medicación administrada.</p>}
            </div>
          </section>
        </div>
      </div>

      {confirming && (
        <ConfirmDialog
          title={TRANSITION_LABEL[confirming]}
          danger={confirming === 'Fallecido'}
          busy={busy}
          message={
            confirming === 'Alta'
              ? `¿Dar de alta a ${d.petName ?? `la mascota #${d.petId}`}?`
              : `¿Registrar el fallecimiento de ${d.petName ?? `la mascota #${d.petId}`}?`
          }
          confirmLabel={TRANSITION_LABEL[confirming]}
          onConfirm={confirmTerminal}
          onCancel={() => setConfirming(null)}
        />
      )}
    </div>
  );
}

function duration(start: string, end: string | null): string {
  const from = new Date(start).getTime();
  const to = end ? new Date(end).getTime() : Date.now();
  const diff = Math.max(0, to - from);
  const hours = Math.floor(diff / 3_600_000);
  const days = Math.floor(hours / 24);
  if (days >= 1) return `${days} d ${hours % 24} h`;
  const mins = Math.floor(diff / 60_000);
  if (hours >= 1) return `${hours} h ${mins % 60} min`;
  return `${mins} min`;
}

function nowLocal(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

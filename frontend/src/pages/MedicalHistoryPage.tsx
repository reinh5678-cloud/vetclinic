import { useEffect, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api, getToken } from '../api/client';
import { useAuth } from '../context/AuthContext';
import {
  addLabResult,
  addPrescription,
  createRecord,
  deleteAttachment,
  deleteLabResult,
  deletePrescription,
  deleteRecord,
  getRecord,
  listRecordsByPet,
  updateLabResult,
  updatePrescription,
  updateRecord,
  uploadAttachment,
  type Attachment,
  type LabResultInput,
  type MedicalRecord,
  type MedicalRecordInput,
  type PrescriptionInput,
} from '../api/medicalRecords';
import type { InventoryItem } from '../api/inventory';

interface Pet { id: number; name: string; species: string; breed: string; }
interface VaccineApplication { id: number; vaccineName: string | null; applicationDate: string; nextDueDate: string | null; isDueSoon: boolean; }

export default function MedicalHistoryPage() {
  const { user, can } = useAuth();
  const canManage = can('medical.manage');
  const [searchParams, setSearchParams] = useSearchParams();

  const [pets, setPets] = useState<Pet[]>([]);
  const [petId, setPetId] = useState<number | null>(() => {
    const value = searchParams.get('petId');
    return value ? Number(value) : null;
  });
  const [appointmentId, setAppointmentId] = useState<number | null>(() => {
    const value = searchParams.get('appointmentId');
    return value ? Number(value) : null;
  });
  const [records, setRecords] = useState<MedicalRecord[]>([]);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const loadRecords = (id: number) =>
    listRecordsByPet(id).then(setRecords).catch((e) => setErr((e as Error).message));

  useEffect(() => {
    api<Pet[]>('/patients/pets').then(setPets).catch(console.error);
  }, []);

  useEffect(() => {
    if (petId) loadRecords(petId);
    else setRecords([]);
  }, [petId]);

  const addRecord = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId) return;
    setErr('');
    setMsg('');
    const form = e.target as HTMLFormElement;
    const data = new FormData(form);
    const input: MedicalRecordInput = {
      petId,
      vetId: user?.role === 'Veterinario' ? user.id : null,
      appointmentId,
      symptoms: String(data.get('symptoms') ?? ''),
      findings: String(data.get('findings') ?? ''),
      diagnosis: String(data.get('diagnosis') ?? ''),
      treatmentPlan: String(data.get('treatmentPlan') ?? ''),
      weightKg: data.get('weightKg') ? Number(data.get('weightKg')) : null,
      temperatureC: data.get('temperatureC') ? Number(data.get('temperatureC')) : null,
    };
    try {
      await createRecord(input);
      form.reset();
      setAppointmentId(null);
      setSearchParams(petId ? { petId: String(petId) } : {}, { replace: true });
      setMsg('Historia clínica registrada.');
      await loadRecords(petId);
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const removeFromList = (id: number) => setRecords((prev) => prev.filter((r) => r.id !== id));

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Historia Clínica</h2>
          <p className="page-subtitle">Consultas, recetas, laboratorio y adjuntos por paciente.</p>
        </div>
      </div>

      <div className="card card-pad">
        <div className="field max-w-md">
          <label className="label">Paciente</label>
          <select
            value={petId ?? ''}
            onChange={(e) => setPetId(e.target.value ? Number(e.target.value) : null)}
            className="select"
          >
            <option value="">-- Seleccionar mascota --</option>
            {pets.map((p) => (
              <option key={p.id} value={p.id}>{p.name} ({p.species} - {p.breed || 'sin raza'})</option>
            ))}
          </select>
        </div>
      </div>

      {petId && (
        <>
          <VaccinesSection petId={petId} />

          {appointmentId && (
            <div className="alert alert-info">
              <p>Registrando la consulta de la cita #{appointmentId}. Al guardar quedará vinculada a esa cita.</p>
            </div>
          )}

          {canManage && (
            <form onSubmit={addRecord} className="card card-pad space-y-3">
              <h3 className="section-title">Nueva consulta</h3>
              <input name="symptoms" placeholder="Síntomas *" className="input" required />
              <input name="findings" placeholder="Hallazgos" className="input" />
              <input name="diagnosis" placeholder="Diagnóstico" className="input" />
              <input name="treatmentPlan" placeholder="Plan de tratamiento" className="input" />
              <div className="grid gap-3 sm:grid-cols-2">
                <input type="number" step="0.1" min="0" max="300" name="weightKg" placeholder="Peso (kg)" className="input" />
                <input type="number" step="0.1" min="0" max="60" name="temperatureC" placeholder="Temperatura (°C)" className="input" />
              </div>
              <button type="submit" className="btn btn-primary">Guardar consulta</button>
            </form>
          )}
        </>
      )}

      {msg && <div className="alert alert-success">{msg}</div>}
      {err && <div className="alert alert-danger">{err}</div>}

      {records.map((r) => (
        <RecordCard
          key={r.id}
          record={r}
          petId={petId!}
          canManage={canManage}
          onDeleted={removeFromList}
        />
      ))}
    </div>
  );
}

function VaccinesSection({ petId }: { petId: number }) {
  const [vaccines, setVaccines] = useState<VaccineApplication[]>([]);
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [inventoryItemId, setInventoryItemId] = useState('');
  const [applicationDate, setApplicationDate] = useState(new Date().toISOString().slice(0, 10));
  const [nextDueDate, setNextDueDate] = useState('');
  const [msg, setMsg] = useState('');

  const reload = () => {
    api<VaccineApplication[]>(`/patients/pets/${petId}/vaccines`).then(setVaccines).catch(console.error);
  };

  useEffect(() => {
    api<VaccineApplication[]>(`/patients/pets/${petId}/vaccines`).then(setVaccines).catch(console.error);
    api<InventoryItem[]>('/inventory')
      .then((all) => {
        const vacunas = all.filter((i) => i.category.toLowerCase() === 'vacunas');
        setItems(vacunas.length > 0 ? vacunas : all);
      })
      .catch(console.error);
  }, [petId]);

  const dueSoon = vaccines.filter((v) => v.isDueSoon);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!inventoryItemId) return;
    await api(`/patients/pets/${petId}/vaccines`, {
      method: 'POST',
      body: JSON.stringify({
        inventoryItemId: Number(inventoryItemId),
        applicationDate: new Date(applicationDate).toISOString(),
        nextDueDate: nextDueDate ? new Date(nextDueDate).toISOString() : null,
      }),
    });
    setInventoryItemId(''); setNextDueDate('');
    setMsg('Vacuna registrada.');
    reload();
  };

  return (
    <div className="card card-pad space-y-4">
      <h3 className="section-title">Vacunas</h3>

      {dueSoon.length > 0 && (
        <div className="alert alert-warning">
          <svg className="mt-0.5 h-5 w-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <p>{dueSoon.length === 1 ? 'Próxima dosis dentro de 30 días o vencida' : 'Hay dosis próximas a vencer o vencidas'}.</p>
        </div>
      )}

      {vaccines.length === 0 ? (
        <p className="text-sm text-slate-400">Sin vacunas registradas.</p>
      ) : (
        <div className="overflow-x-auto rounded-xl border border-slate-200">
          <table className="table">
            <thead>
              <tr>
                <th>Vacuna</th>
                <th>Aplicada</th>
                <th>Próxima dosis</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {vaccines.map((v) => (
                <tr key={v.id}>
                  <td className="font-medium text-slate-800">{v.vaccineName ?? 'Vacuna'}</td>
                  <td>{new Date(v.applicationDate).toLocaleDateString()}</td>
                  <td>{v.nextDueDate ? new Date(v.nextDueDate).toLocaleDateString() : '—'}</td>
                  <td>
                    {v.isDueSoon
                      ? <span className="badge badge-danger">PRÓXIMA DOSIS</span>
                      : <span className="badge badge-success">AL DÍA</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <form onSubmit={submit} className="flex flex-wrap items-end gap-3">
        <div className="field min-w-44">
          <label className="label">Vacuna</label>
          <select value={inventoryItemId} onChange={(e) => setInventoryItemId(e.target.value)} className="select" required>
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
        <button type="submit" className="btn btn-primary">Registrar vacuna</button>
      </form>
      {msg && <div className="alert alert-success">{msg}</div>}
    </div>
  );
}

function PrescriptionForm({
  initial,
  onSubmit,
  onCancel,
}: {
  initial?: PrescriptionInput;
  onSubmit: (input: PrescriptionInput) => void;
  onCancel: () => void;
}) {
  const submit = (e: FormEvent) => {
    e.preventDefault();
    const data = new FormData(e.target as HTMLFormElement);
    onSubmit({
      medicationName: String(data.get('medicationName') ?? ''),
      dosage: String(data.get('dosage') ?? ''),
      frequency: String(data.get('frequency') ?? ''),
      duration: String(data.get('duration') ?? ''),
      instructions: String(data.get('instructions') ?? ''),
    });
  };
  return (
    <form onSubmit={submit} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
      <input name="medicationName" defaultValue={initial?.medicationName} placeholder="Medicamento *" className="input" required />
      <input name="dosage" defaultValue={initial?.dosage} placeholder="Dosis" className="input" />
      <input name="frequency" defaultValue={initial?.frequency} placeholder="Frecuencia (ej: cada 12h)" className="input" />
      <input name="duration" defaultValue={initial?.duration} placeholder="Duración (ej: 7 días)" className="input" />
      <input name="instructions" defaultValue={initial?.instructions} placeholder="Instrucciones" className="input" />
      <div className="flex gap-2">
        <button type="submit" className="btn btn-primary btn-sm">Guardar receta</button>
        <button type="button" className="btn btn-secondary btn-sm" onClick={onCancel}>Cancelar</button>
      </div>
    </form>
  );
}

function LabResultForm({
  initial,
  onSubmit,
  onCancel,
}: {
  initial?: LabResultInput;
  onSubmit: (input: LabResultInput) => void;
  onCancel: () => void;
}) {
  const submit = (e: FormEvent) => {
    e.preventDefault();
    const data = new FormData(e.target as HTMLFormElement);
    onSubmit({
      testType: String(data.get('testType') ?? ''),
      result: String(data.get('result') ?? ''),
      referenceRange: String(data.get('referenceRange') ?? ''),
      isAbnormal: data.get('isAbnormal') === 'on',
    });
  };
  return (
    <form onSubmit={submit} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
      <input name="testType" defaultValue={initial?.testType} placeholder="Tipo de prueba (ej: Hemograma) *" className="input" required />
      <input name="result" defaultValue={initial?.result} placeholder="Resultado (ej: GB 18.2)" className="input" />
      <input name="referenceRange" defaultValue={initial?.referenceRange} placeholder="Rango de referencia" className="input" />
      <label className="flex items-center gap-2 text-sm text-slate-600">
        <input type="checkbox" name="isAbnormal" defaultChecked={initial?.isAbnormal} className="checkbox" /> Marcado como anormal
      </label>
      <div className="flex gap-2">
        <button type="submit" className="btn btn-primary btn-sm">Guardar análisis</button>
        <button type="button" className="btn btn-secondary btn-sm" onClick={onCancel}>Cancelar</button>
      </div>
    </form>
  );
}

function RecordCard({
  record: initialRecord,
  petId,
  canManage,
  onDeleted,
}: {
  record: MedicalRecord;
  petId: number;
  canManage: boolean;
  onDeleted: (id: number) => void;
}) {
  const [record, setRecord] = useState(initialRecord);
  const [editing, setEditing] = useState(false);
  const [showRx, setShowRx] = useState(false);
  const [editingRxId, setEditingRxId] = useState<number | null>(null);
  const [showLab, setShowLab] = useState(false);
  const [editingLabId, setEditingLabId] = useState<number | null>(null);
  const [showAtt, setShowAtt] = useState(false);
  const [err, setErr] = useState('');

  useEffect(() => { setRecord(initialRecord); }, [initialRecord]);

  const reload = async () => setRecord(await getRecord(record.id));

  const run = async (fn: () => Promise<unknown>) => {
    setErr('');
    try {
      await fn();
      await reload();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const submitEdit = (e: FormEvent) => {
    e.preventDefault();
    const data = new FormData(e.target as HTMLFormElement);
    run(async () => {
      const input: MedicalRecordInput = {
        petId,
        vetId: record.vetId,
        appointmentId: null,
        symptoms: String(data.get('symptoms') ?? ''),
        findings: String(data.get('findings') ?? ''),
        diagnosis: String(data.get('diagnosis') ?? ''),
        treatmentPlan: String(data.get('treatmentPlan') ?? ''),
        weightKg: data.get('weightKg') ? Number(data.get('weightKg')) : null,
        temperatureC: data.get('temperatureC') ? Number(data.get('temperatureC')) : null,
      };
      await updateRecord(record.id, input);
      setEditing(false);
    });
  };

  const removeRecord = async () => {
    if (!window.confirm(`¿Eliminar la consulta #${record.id}? Esta acción no se puede deshacer.`)) return;
    setErr('');
    try {
      await deleteRecord(record.id);
      onDeleted(record.id);
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const submitRx = (input: PrescriptionInput) =>
    run(async () => { await addPrescription(record.id, input); setShowRx(false); });

  const saveRx = (id: number, input: PrescriptionInput) =>
    run(async () => { await updatePrescription(record.id, id, input); setEditingRxId(null); });

  const removeRx = (id: number) => run(() => deletePrescription(record.id, id));

  const submitLab = (input: LabResultInput) =>
    run(async () => { await addLabResult(record.id, input); setShowLab(false); });

  const saveLab = (id: number, input: LabResultInput) =>
    run(async () => { await updateLabResult(record.id, id, input); setEditingLabId(null); });

  const removeLab = (id: number) => run(() => deleteLabResult(record.id, id));

  const submitAtt = async (e: FormEvent) => {
    e.preventDefault();
    const formEl = e.target as HTMLFormElement;
    const fileInput = formEl.elements.namedItem('file') as HTMLInputElement;
    const file = fileInput?.files?.[0];
    if (!file) return;
    setErr('');
    try {
      await uploadAttachment(record.id, file);
      formEl.reset();
      setShowAtt(false);
      await reload();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const removeAtt = (id: number) => {
    if (!window.confirm('¿Eliminar este adjunto?')) return;
    run(() => deleteAttachment(record.id, id));
  };

  const openAttachment = async (a: Attachment) => {
    const res = await fetch(`/api/medical-records/${record.id}/attachments/${a.id}/download`, {
      headers: { Authorization: `Bearer ${getToken()}` },
    });
    if (!res.ok) {
      alert(`No se pudo abrir el archivo (${res.status}).`);
      return;
    }
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    if (a.contentType.startsWith('image/')) {
      window.open(url, '_blank');
    } else {
      const link = document.createElement('a');
      link.href = url;
      link.download = a.fileName;
      link.click();
    }
  };

  return (
    <div className="card overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-teal-100 bg-teal-50 px-5 py-3">
        <strong className="text-slate-800">Consulta #{record.id}</strong>
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500">
            {new Date(record.recordDate).toLocaleString()}{record.vetName ? ` · ${record.vetName}` : ''}
          </span>
          {canManage && (
            <>
              <button type="button" className="btn btn-secondary btn-xs" onClick={() => setEditing(!editing)}>
                {editing ? 'Cancelar' : 'Editar'}
              </button>
              <button type="button" className="btn btn-danger btn-xs" onClick={removeRecord}>Eliminar</button>
            </>
          )}
        </div>
      </div>

      <div className="space-y-3 p-5">
        {err && <div className="alert alert-danger">{err}</div>}

        {editing ? (
          <form onSubmit={submitEdit} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
            <input name="symptoms" defaultValue={record.symptoms} placeholder="Síntomas *" className="input" required />
            <input name="findings" defaultValue={record.findings} placeholder="Hallazgos" className="input" />
            <input name="diagnosis" defaultValue={record.diagnosis} placeholder="Diagnóstico" className="input" />
            <input name="treatmentPlan" defaultValue={record.treatmentPlan} placeholder="Plan de tratamiento" className="input" />
            <div className="grid gap-3 sm:grid-cols-2">
              <input type="number" step="0.1" min="0" max="300" name="weightKg" defaultValue={record.weightKg ?? ''} placeholder="Peso (kg)" className="input" />
              <input type="number" step="0.1" min="0" max="60" name="temperatureC" defaultValue={record.temperatureC ?? ''} placeholder="Temperatura (°C)" className="input" />
            </div>
            <button type="submit" className="btn btn-primary btn-sm">Guardar cambios</button>
          </form>
        ) : (
          <dl className="grid gap-3 sm:grid-cols-2">
            <div><dt className="text-xs font-medium text-slate-500">Síntomas</dt><dd className="text-sm text-slate-700">{record.symptoms || '—'}</dd></div>
            <div><dt className="text-xs font-medium text-slate-500">Hallazgos</dt><dd className="text-sm text-slate-700">{record.findings || '—'}</dd></div>
            <div><dt className="text-xs font-medium text-slate-500">Diagnóstico</dt><dd className="text-sm text-slate-700">{record.diagnosis || '—'}</dd></div>
            <div><dt className="text-xs font-medium text-slate-500">Tratamiento</dt><dd className="text-sm text-slate-700">{record.treatmentPlan || '—'}</dd></div>
            <div className="sm:col-span-2 flex flex-wrap gap-2">
              <span className="badge badge-neutral">Peso: {record.weightKg != null ? `${record.weightKg} kg` : '—'}</span>
              <span className="badge badge-neutral">Temperatura: {record.temperatureC != null ? `${record.temperatureC} °C` : '—'}</span>
            </div>
          </dl>
        )}

        {canManage && (
          <div className="flex flex-wrap gap-2 border-t border-slate-100 pt-3">
            <button className="btn btn-secondary btn-sm" onClick={() => setShowRx(!showRx)}>{showRx ? 'Cancelar' : '+ Receta'}</button>
            <button className="btn btn-secondary btn-sm" onClick={() => setShowLab(!showLab)}>{showLab ? 'Cancelar' : '+ Análisis de laboratorio'}</button>
            <button className="btn btn-secondary btn-sm" onClick={() => setShowAtt(!showAtt)}>{showAtt ? 'Cancelar' : '+ Adjunto'}</button>
          </div>
        )}

        {showRx && <PrescriptionForm onSubmit={submitRx} onCancel={() => setShowRx(false)} />}
        {showLab && <LabResultForm onSubmit={submitLab} onCancel={() => setShowLab(false)} />}

        {showAtt && (
          <form onSubmit={submitAtt} className="space-y-3 rounded-xl border border-slate-200 bg-slate-50/60 p-4">
            <input type="file" name="file" className="input" required />
            <p className="text-[13px] text-slate-500">Adjunta rayos X, ecografías u otros documentos (máx. 20 MB; imágenes, PDF o documentos de oficina).</p>
            <button type="submit" className="btn btn-primary btn-sm">Subir archivo</button>
          </form>
        )}

        <div className="grid gap-5 border-t border-slate-100 pt-3 lg:grid-cols-3">
          <section>
            <h4 className="mb-2 text-sm font-semibold text-slate-700">Recetas ({record.prescriptions.length})</h4>
            <div className="space-y-2">
              {record.prescriptions.map((rx) => editingRxId === rx.id ? (
                <PrescriptionForm
                  key={rx.id}
                  initial={{ medicationName: rx.medicationName, dosage: rx.dosage, frequency: rx.frequency, duration: rx.duration, instructions: rx.instructions }}
                  onSubmit={(input) => saveRx(rx.id, input)}
                  onCancel={() => setEditingRxId(null)}
                />
              ) : (
                <div key={rx.id} className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <strong className="text-slate-700">{rx.medicationName}</strong>{' '}
                      <span className="text-slate-600">— {rx.dosage}, {rx.frequency} {rx.duration && `por ${rx.duration}`}</span>
                      {rx.instructions && <div className="text-[13px] text-slate-500">{rx.instructions}</div>}
                    </div>
                    {canManage && (
                      <div className="flex gap-1">
                        <button type="button" className="btn btn-ghost btn-xs" onClick={() => setEditingRxId(rx.id)}>Editar</button>
                        <button type="button" className="btn btn-ghost btn-xs text-rose-600" onClick={() => removeRx(rx.id)}>Eliminar</button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
              {record.prescriptions.length === 0 && <p className="text-sm text-slate-400">Sin recetas.</p>}
            </div>
          </section>
          <section>
            <h4 className="mb-2 text-sm font-semibold text-slate-700">Laboratorio ({record.labResults.length})</h4>
            <div className="space-y-2">
              {record.labResults.map((l) => editingLabId === l.id ? (
                <LabResultForm
                  key={l.id}
                  initial={{ testType: l.testType, result: l.result, referenceRange: l.referenceRange, isAbnormal: l.isAbnormal }}
                  onSubmit={(input) => saveLab(l.id, input)}
                  onCancel={() => setEditingLabId(null)}
                />
              ) : (
                <div key={l.id} className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <strong className="text-slate-700">{l.testType}</strong>{' '}
                      <span className="text-slate-600">: {l.result} {l.referenceRange && `(ref ${l.referenceRange})`}</span>
                      {l.isAbnormal && <span className="ml-1 font-medium text-rose-600">⚠ anormal</span>}
                    </div>
                    {canManage && (
                      <div className="flex gap-1">
                        <button type="button" className="btn btn-ghost btn-xs" onClick={() => setEditingLabId(l.id)}>Editar</button>
                        <button type="button" className="btn btn-ghost btn-xs text-rose-600" onClick={() => removeLab(l.id)}>Eliminar</button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
              {record.labResults.length === 0 && <p className="text-sm text-slate-400">Sin análisis.</p>}
            </div>
          </section>
          <section>
            <h4 className="mb-2 text-sm font-semibold text-slate-700">Adjuntos ({record.attachments.length})</h4>
            <div className="space-y-2">
              {record.attachments.map((a) => (
                <div key={a.id} className="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
                  <svg className="h-4 w-4 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M18.375 12.739l-7.693 7.693a4.5 4.5 0 01-6.364-6.364l10.94-10.94A3 3 0 1119.5 7.372L8.552 18.32m.009-.01l-.01.01m5.699-9.941l-7.81 7.81a1.5 1.5 0 002.112 2.13" />
                  </svg>
                  <span className="min-w-0 flex-1 truncate text-slate-700">{a.fileName}</span>
                  <span className="text-[13px] text-slate-500">{(a.sizeBytes / 1024).toFixed(1)} KB</span>
                  <button type="button" className="btn btn-secondary btn-xs" onClick={() => openAttachment(a)}>
                    {a.contentType.startsWith('image/') ? 'Ver' : 'Descargar'}
                  </button>
                  {canManage && (
                    <button type="button" className="btn btn-ghost btn-xs text-rose-600" onClick={() => removeAtt(a.id)}>Eliminar</button>
                  )}
                </div>
              ))}
              {record.attachments.length === 0 && <p className="text-sm text-slate-400">Sin adjuntos.</p>}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
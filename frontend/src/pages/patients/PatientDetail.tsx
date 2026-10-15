import { useEffect, useState } from 'react';
import { api } from '../../api/client';
import { listVaccines, type Pet, type VaccineApplication } from '../../api/patients';
import { listRecordsByPet, type MedicalRecord } from '../../api/medicalRecords';

interface Appointment {
  id: number;
  startTime: string;
  status: string;
  reason: string;
  vetName: string | null;
}

const statusBadge: Record<string, string> = {
  Programada: 'badge-info',
  EnEspera: 'badge-warning',
  EnConsulta: 'badge-info',
  Completada: 'badge-success',
  Cancelada: 'badge-neutral',
  NoAsistio: 'badge-danger',
};

function fmtDate(value: string | null): string {
  return value ? new Date(value).toLocaleDateString() : '—';
}

export default function PatientDetail({ pet }: { pet: Pet }) {
  const [vaccines, setVaccines] = useState<VaccineApplication[]>([]);
  const [records, setRecords] = useState<MedicalRecord[]>([]);
  const [appointments, setAppointments] = useState<Appointment[]>([]);

  useEffect(() => {
    listVaccines(pet.id).then(setVaccines).catch(console.error);
    listRecordsByPet(pet.id).then(setRecords).catch(console.error);
    api<Appointment[]>(`/appointments/pet/${pet.id}`).then(setAppointments).catch(console.error);
  }, [pet.id]);

  const dueSoon = vaccines.filter((v) => v.isDueSoon);

  return (
    <div className="space-y-4">
      <div className="grid gap-4 lg:grid-cols-[minmax(260px,1fr)_minmax(300px,1.4fr)]">
        <section className="card p-4">
          <h4 className="mb-3 text-sm font-semibold text-slate-700">Datos del paciente</h4>
          <dl className="grid grid-cols-2 gap-3 text-sm">
            <div><dt className="text-xs text-slate-500">Especie</dt><dd className="text-slate-700">{pet.species}</dd></div>
            <div><dt className="text-xs text-slate-500">Raza</dt><dd className="text-slate-700">{pet.breed || '—'}</dd></div>
            <div><dt className="text-xs text-slate-500">Sexo</dt><dd className="text-slate-700">{pet.sex}</dd></div>
            <div><dt className="text-xs text-slate-500">Nacimiento</dt><dd className="text-slate-700">{fmtDate(pet.birthDate)}</dd></div>
            <div><dt className="text-xs text-slate-500">Edad</dt><dd className="text-slate-700">{pet.ageYears != null ? `${pet.ageYears} años` : '—'}</dd></div>
            <div><dt className="text-xs text-slate-500">Peso</dt><dd className="text-slate-700">{pet.weightKg} kg</dd></div>
            <div><dt className="text-xs text-slate-500">Color</dt><dd className="text-slate-700">{pet.color || '—'}</dd></div>
            <div><dt className="text-xs text-slate-500">Esterilizado</dt><dd className="text-slate-700">{pet.isSterilized ? 'Sí' : 'No'}</dd></div>
            <div className="col-span-2"><dt className="text-xs text-slate-500">Tutor</dt><dd className="text-slate-700">{pet.tutorName ?? 'Sin asignar'}</dd></div>
          </dl>
        </section>

        <section className="card p-4">
          <h4 className="mb-3 text-sm font-semibold text-slate-700">
            Vacunas ({vaccines.length})
            {dueSoon.length > 0 && <span className="ml-2 badge badge-warning">{dueSoon.length} próxima(s)/vencida(s)</span>}
          </h4>
          {vaccines.length === 0 ? (
            <p className="text-sm text-slate-400">Sin vacunas registradas.</p>
          ) : (
            <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-100">
                    <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Vacuna</th>
                    <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Aplicada</th>
                    <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Próxima</th>
                    <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Estado</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {vaccines.map((v) => (
                    <tr key={v.id}>
                      <td className="px-3 py-2 text-slate-700">{v.vaccineName ?? 'Vacuna'}</td>
                      <td className="px-3 py-2 text-slate-600">{fmtDate(v.applicationDate)}</td>
                      <td className="px-3 py-2 text-slate-600">{fmtDate(v.nextDueDate)}</td>
                      <td className="px-3 py-2">
                        {v.isDueSoon
                          ? <span className="badge badge-danger">PRÓXIMA</span>
                          : <span className="badge badge-success">AL DÍA</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <section className="card p-4">
          <h4 className="mb-3 text-sm font-semibold text-slate-700">Historia clínica ({records.length})</h4>
          {records.length === 0 ? (
            <p className="text-sm text-slate-400">Sin consultas registradas.</p>
          ) : (
            <ul className="space-y-2">
              {records.slice(0, 5).map((r) => (
                <li key={r.id} className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-slate-500">{new Date(r.recordDate).toLocaleDateString()}{r.vetName ? ` · ${r.vetName}` : ''}</span>
                  </div>
                  <div className="text-slate-700">{r.diagnosis || r.symptoms || 'Consulta'}</div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="card p-4">
          <h4 className="mb-3 text-sm font-semibold text-slate-700">Citas ({appointments.length})</h4>
          {appointments.length === 0 ? (
            <p className="text-sm text-slate-400">Sin citas registradas.</p>
          ) : (
            <ul className="space-y-2">
              {appointments.map((a) => (
                <li key={a.id} className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
                  <div>
                    <div className="text-slate-700">{a.reason}</div>
                    <div className="text-xs text-slate-500">{new Date(a.startTime).toLocaleString()}{a.vetName ? ` · ${a.vetName}` : ''}</div>
                  </div>
                  <span className={`badge ${statusBadge[a.status] ?? 'badge-neutral'}`}>{a.status}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}

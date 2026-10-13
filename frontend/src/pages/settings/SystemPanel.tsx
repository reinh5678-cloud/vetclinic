import { useEffect, useState } from 'react';
import { api } from '../../api/client';

interface SystemSettings {
  taxRate: number;
  jwtExpirationHours: number;
  attachmentsPath: string;
  maxAttachmentMb: number;
  expiringSoonDays: number;
  environment: string;
  version: string;
}

export default function SystemPanel() {
  const [system, setSystem] = useState<SystemSettings | null>(null);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [taxInput, setTaxInput] = useState('');

  useEffect(() => {
    api<SystemSettings>('/settings/system')
      .then((s) => {
        setSystem(s);
        setTaxInput((s.taxRate * 100).toString());
      })
      .catch((e) => setErr((e as Error).message));
  }, []);

  const notify = (text: string) => {
    setErr('');
    setMsg(text);
  };

  const saveTax = async () => {
    const percent = Number(taxInput.replace(',', '.'));
    if (!Number.isFinite(percent) || percent < 0 || percent > 100) {
      setErr('El IVA debe ser un número entre 0 y 100.');
      return;
    }
    setErr('');
    setMsg('');
    try {
      await api('/settings/billing', { method: 'PUT', body: JSON.stringify({ taxRate: percent / 100 }) });
      notify(`IVA actualizado a ${percent}%.`);
      const s = await api<SystemSettings>('/settings/system');
      setSystem(s);
      setTaxInput((s.taxRate * 100).toString());
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  return (
    <section className="card">
      {err && <div className="alert alert-error m-4">{err}</div>}
      {msg && <div className="alert alert-success m-4">{msg}</div>}

      <div className="card-header">
        <div>
          <h3 className="section-title">Parámetros del sistema</h3>
          <p className="mt-0.5 text-xs text-slate-500">Valores generales de la aplicación.</p>
        </div>
      </div>
      <div className="card-body">
        {system ? (
          <div className="grid gap-5 sm:grid-cols-2">
            <div className="field">
              <label className="label">IVA (%)</label>
              <div className="flex gap-2">
                <input value={taxInput} onChange={(e) => setTaxInput(e.target.value)} className="input w-32" />
                <button onClick={saveTax} className="btn btn-primary">Guardar</button>
              </div>
            </div>
            <dl className="grid grid-cols-2 gap-x-4 gap-y-3 text-sm sm:col-span-2 lg:grid-cols-3">
              <div><dt className="text-xs text-slate-500">Entorno</dt><dd className="font-medium text-slate-800">{system.environment}</dd></div>
              <div><dt className="text-xs text-slate-500">Versión</dt><dd className="font-medium text-slate-800">{system.version}</dd></div>
              <div><dt className="text-xs text-slate-500">Expiración de sesión</dt><dd className="font-medium text-slate-800">{system.jwtExpirationHours} h</dd></div>
              <div><dt className="text-xs text-slate-500">Ruta de adjuntos</dt><dd className="font-medium text-slate-800 break-all">{system.attachmentsPath}</dd></div>
              <div><dt className="text-xs text-slate-500">Tamaño máx. adjunto</dt><dd className="font-medium text-slate-800">{system.maxAttachmentMb} MB</dd></div>
              <div><dt className="text-xs text-slate-500">Aviso «próximo a vencer»</dt><dd className="font-medium text-slate-800">{system.expiringSoonDays} días</dd></div>
            </dl>
          </div>
        ) : (
          <p className="text-sm text-slate-400">Cargando parámetros…</p>
        )}
      </div>
    </section>
  );
}

import { useEffect, useState } from 'react';
import SettingsSection from './SettingsSection';
import { parseList } from './useSettingsSection';
import {
  getNotifications,
  saveNotifications,
  testNotifications,
  type NotificationSettings,
  type UpdateNotificationsInput,
} from '../../api/settings';

export default function NotificationsPanel() {
  const [value, setValue] = useState<NotificationSettings | null>(null);
  const [password, setPassword] = useState('');
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');
  const [busy, setBusy] = useState(false);
  const [testing, setTesting] = useState(false);

  useEffect(() => {
    getNotifications()
      .then((v) => {
        setValue(v);
      })
      .catch((e) => setErr((e as Error).message));
  }, []);

  const update = <K extends keyof UpdateNotificationsInput>(key: K, v: UpdateNotificationsInput[K]) => {
    setValue((prev) => (prev ? { ...prev, [key]: v } : prev));
  };

  const save = async () => {
    if (!value) return;
    setErr('');
    setMsg('');
    setBusy(true);
    try {
      const body: UpdateNotificationsInput = {
        enabled: value.enabled,
        smtpHost: value.smtpHost,
        smtpPort: value.smtpPort,
        smtpUser: value.smtpUser,
        smtpPassword: password.length > 0 ? password : null,
        useTls: value.useTls,
        fromName: value.fromName,
        fromEmail: value.fromEmail,
        channels: value.channels,
        vaccineReminderDaysBefore: value.vaccineReminderDaysBefore,
      };
      const saved = await saveNotifications(body);
      setValue(saved);
      setPassword('');
      setMsg('Cambios guardados.');
    } catch (e) {
      setErr((e as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const test = async () => {
    setErr('');
    setMsg('');
    setTesting(true);
    try {
      const res = await testNotifications();
      setMsg(res.message);
    } catch (e) {
      setErr((e as Error).message);
    } finally {
      setTesting(false);
    }
  };

  return (
    <SettingsSection
      title="Notificaciones"
      subtitle="Servidor SMTP, remitente y recordatorios automáticos."
      err={err}
      msg={msg}
      onSave={save}
      busy={busy}
    >
      {value ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="flex items-center gap-2 text-sm text-slate-600 sm:col-span-2">
            <input type="checkbox" className="checkbox" checked={value.enabled} onChange={(e) => update('enabled', e.target.checked)} />
            Notificaciones activadas
          </label>

          <div className="field">
            <label className="label">Servidor SMTP</label>
            <input className="input" value={value.smtpHost} onChange={(e) => update('smtpHost', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Puerto SMTP</label>
            <input type="number" min={1} max={65535} className="input" value={value.smtpPort} onChange={(e) => update('smtpPort', Number(e.target.value))} />
          </div>
          <div className="field">
            <label className="label">Usuario SMTP</label>
            <input className="input" value={value.smtpUser} onChange={(e) => update('smtpUser', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Contraseña SMTP</label>
            <input
              type="password"
              className="input"
              placeholder={value.smtpPasswordSet ? '•••••••• (configurada)' : 'Sin contraseña'}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-600">
            <input type="checkbox" className="checkbox" checked={value.useTls} onChange={(e) => update('useTls', e.target.checked)} />
            Usar TLS
          </label>

          <div className="field">
            <label className="label">Nombre del remitente</label>
            <input className="input" value={value.fromName} onChange={(e) => update('fromName', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Email del remitente</label>
            <input className="input" value={value.fromEmail} onChange={(e) => update('fromEmail', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Días antes del vencimiento de vacuna</label>
            <input type="number" min={1} max={365} className="input" value={value.vaccineReminderDaysBefore} onChange={(e) => update('vaccineReminderDaysBefore', Number(e.target.value))} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Canales (separados por coma)</label>
            <input className="input" value={value.channels.join(', ')} onChange={(e) => update('channels', parseList(e.target.value))} />
          </div>

          <div className="sm:col-span-2">
            <button onClick={test} disabled={testing} className="btn btn-secondary btn-sm">
              {testing ? 'Enviando…' : 'Enviar prueba'}
            </button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-400">Cargando…</p>
      )}
    </SettingsSection>
  );
}

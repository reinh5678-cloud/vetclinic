import SettingsSection from './SettingsSection';
import NumberInput from './NumberInput';
import { useSettingsSection } from './useSettingsSection';
import type { SecuritySettings } from '../../api/settings';

export default function SecurityPanel() {
  const { value, update, save, err, msg, busy } = useSettingsSection<SecuritySettings>('security');

  return (
    <SettingsSection
      title="Seguridad"
      subtitle="Políticas de contraseñas, PIN y sesiones."
      err={err}
      msg={msg}
      onSave={save}
      busy={busy}
    >
      {value ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="field">
            <label className="label">Longitud mínima de contraseña</label>
            <NumberInput min={1} max={128} className="input" value={value.passwordMinLength} onChange={(n) => update('passwordMinLength', n)} />
          </div>
          <div className="field">
            <label className="label">Longitud del PIN</label>
            <NumberInput min={1} max={12} className="input" value={value.pinLength} onChange={(n) => update('pinLength', n)} />
          </div>
          <div className="field">
            <label className="label">Duración de sesión (horas)</label>
            <NumberInput min={1} max={720} className="input" value={value.sessionHours} onChange={(n) => update('sessionHours', n)} />
          </div>
          <div className="field">
            <label className="label">Intentos fallidos máximos</label>
            <NumberInput min={1} max={50} className="input" value={value.maxFailedAttempts} onChange={(n) => update('maxFailedAttempts', n)} />
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-600">
            <input type="checkbox" className="checkbox" checked={value.requireLetter} onChange={(e) => update('requireLetter', e.target.checked)} />
            Requiere al menos una letra
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-600">
            <input type="checkbox" className="checkbox" checked={value.requireDigit} onChange={(e) => update('requireDigit', e.target.checked)} />
            Requiere al menos un dígito
          </label>
        </div>
      ) : (
        <p className="text-sm text-slate-400">Cargando…</p>
      )}
    </SettingsSection>
  );
}

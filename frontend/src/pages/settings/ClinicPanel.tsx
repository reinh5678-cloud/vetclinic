import SettingsSection from './SettingsSection';
import { useSettingsSection } from './useSettingsSection';
import type { ClinicSettings } from '../../api/settings';

const documentTypes = [
  { code: 'RUC', label: 'RUC (empresa)', placeholder: '20123456789', maxLength: 11, numeric: true, hint: '11 dígitos' },
  { code: 'DNI', label: 'DNI (persona natural)', placeholder: '12345678', maxLength: 8, numeric: true, hint: '8 dígitos' },
  { code: 'CE', label: 'Carné de extranjería', placeholder: '001234567', maxLength: 12, numeric: false, hint: '9 a 12 caracteres' },
  { code: 'PASSPORT', label: 'Pasaporte', placeholder: 'P1234567', maxLength: 12, numeric: false, hint: '6 a 12 caracteres' },
];

export default function ClinicPanel() {
  const { value, update, save, err, msg, busy } = useSettingsSection<ClinicSettings>('clinic');
  const taxType = documentTypes.find((t) => t.code === value?.taxIdType) ?? documentTypes[0];

  return (
    <SettingsSection
      title="Clínica"
      subtitle="Datos de la clínica que se muestran en la aplicación y en los documentos."
      err={err}
      msg={msg}
      onSave={save}
      busy={busy}
    >
      {value ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="field">
            <label className="label">Nombre comercial</label>
            <input className="input" value={value.name} onChange={(e) => update('name', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Razón social</label>
            <input className="input" value={value.legalName} onChange={(e) => update('legalName', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Tipo de documento</label>
            <select className="select" value={taxType.code} onChange={(e) => update('taxIdType', e.target.value)}>
              {documentTypes.map((t) => (
                <option key={t.code} value={t.code}>{t.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label className="label">Número de documento</label>
            <input
              className="input"
              value={value.taxId}
              inputMode={taxType.numeric ? 'numeric' : 'text'}
              maxLength={taxType.maxLength}
              placeholder={taxType.placeholder}
              onChange={(e) => update('taxId', e.target.value)}
            />
            <span className="text-xs text-slate-400">{taxType.hint}</span>
          </div>
          <div className="field">
            <label className="label">Teléfono</label>
            <input className="input" value={value.phone} onChange={(e) => update('phone', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Email</label>
            <input className="input" value={value.email} onChange={(e) => update('email', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Dirección</label>
            <input className="input" value={value.address} onChange={(e) => update('address', e.target.value)} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Logo (URL o data URI)</label>
            <input className="input" value={value.logoDataUrl ?? ''} onChange={(e) => update('logoDataUrl', e.target.value || null)} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Encabezado de facturas</label>
            <input className="input" value={value.invoiceHeader} onChange={(e) => update('invoiceHeader', e.target.value)} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Pie de facturas</label>
            <input className="input" value={value.invoiceFooter} onChange={(e) => update('invoiceFooter', e.target.value)} />
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-400">Cargando…</p>
      )}
    </SettingsSection>
  );
}

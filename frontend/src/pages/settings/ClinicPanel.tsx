import SettingsSection from './SettingsSection';
import { useSettingsSection } from './useSettingsSection';
import type { ClinicSettings } from '../../api/settings';

export default function ClinicPanel() {
  const { value, update, save, err, msg, busy } = useSettingsSection<ClinicSettings>('clinic');

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
            <label className="label">CUIT / NIF</label>
            <input className="input" value={value.taxId} onChange={(e) => update('taxId', e.target.value)} />
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

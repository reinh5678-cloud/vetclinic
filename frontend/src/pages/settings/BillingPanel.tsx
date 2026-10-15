import SettingsSection from './SettingsSection';
import { parseList, useSettingsSection } from './useSettingsSection';
import type { BillingSettings } from '../../api/settings';

export default function BillingPanel() {
  const { value, update, save, err, msg, busy } = useSettingsSection<BillingSettings>('billing');

  return (
    <SettingsSection
      title="Facturación"
      subtitle="Impuestos, moneda y numeración usados al emitir facturas."
      err={err}
      msg={msg}
      onSave={save}
      busy={busy}
    >
      {value ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="field">
            <label className="label">IVA (%)</label>
            <input
              type="number"
              min={0}
              max={100}
              step="0.01"
              className="input"
              value={(value.taxRate * 100).toString()}
              onChange={(e) => update('taxRate', Number(e.target.value) / 100)}
            />
          </div>
          <div className="field">
            <label className="label">Prefijo de numeración</label>
            <input className="input" value={value.invoicePrefix} onChange={(e) => update('invoicePrefix', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Moneda</label>
            <input className="input" value={value.currency} onChange={(e) => update('currency', e.target.value)} />
          </div>
          <div className="field">
            <label className="label">Símbolo</label>
            <input className="input" value={value.currencySymbol} onChange={(e) => update('currencySymbol', e.target.value)} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Medios de pago (separados por coma)</label>
            <input
              className="input"
              value={value.paymentMethods.join(', ')}
              onChange={(e) => update('paymentMethods', parseList(e.target.value))}
            />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Texto legal</label>
            <textarea
              className="input"
              rows={3}
              value={value.legalText}
              onChange={(e) => update('legalText', e.target.value)}
            />
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-400">Cargando…</p>
      )}
    </SettingsSection>
  );
}

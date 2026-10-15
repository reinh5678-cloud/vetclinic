import SettingsSection from './SettingsSection';
import { parseList, useSettingsSection } from './useSettingsSection';
import type { InventorySettings } from '../../api/settings';

export default function InventorySettingsPanel() {
  const { value, update, save, err, msg, busy } = useSettingsSection<InventorySettings>('inventory');

  return (
    <SettingsSection
      title="Inventario y farmacia"
      subtitle="Avisos de vencimiento, categorías y unidades disponibles."
      err={err}
      msg={msg}
      onSave={save}
      busy={busy}
    >
      {value ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="field">
            <label className="label">Aviso «próximo a vencer» (días)</label>
            <input type="number" min={1} max={365} className="input" value={value.expiringSoonDays} onChange={(e) => update('expiringSoonDays', Number(e.target.value))} />
          </div>
          <div className="field">
            <label className="label">Stock mínimo por defecto</label>
            <input type="number" min={0} className="input" value={value.defaultMinStock} onChange={(e) => update('defaultMinStock', Number(e.target.value))} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Categorías (separadas por coma)</label>
            <input className="input" value={value.categories.join(', ')} onChange={(e) => update('categories', parseList(e.target.value))} />
          </div>
          <div className="field sm:col-span-2">
            <label className="label">Unidades (separadas por coma)</label>
            <input className="input" value={value.units.join(', ')} onChange={(e) => update('units', parseList(e.target.value))} />
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-400">Cargando…</p>
      )}
    </SettingsSection>
  );
}

import { useCallback, useEffect, useState } from 'react';
import SettingsSection from './SettingsSection';
import NumberInput from './NumberInput';
import StringListTable from './StringListTable';
import { useSettingsSection } from './useSettingsSection';
import { getInventoryUsage, type InventorySettings, type InventoryUsage } from '../../api/settings';

function UsageBadge({ count }: { count: number | undefined }) {
  if (count === undefined) return <span className="text-slate-400">—</span>;
  if (count === 0) return <span className="badge badge-neutral">Sin uso</span>;
  return <span className="badge badge-info">{count}</span>;
}

export default function InventorySettingsPanel() {
  const { value, update, save, err, msg, busy } = useSettingsSection<InventorySettings>('inventory');
  const [usage, setUsage] = useState<InventoryUsage | null>(null);

  const loadUsage = useCallback(() => {
    getInventoryUsage()
      .then(setUsage)
      .catch(() => setUsage(null));
  }, []);

  useEffect(() => {
    loadUsage();
  }, [loadUsage]);

  const handleSave = () => {
    void save().then(loadUsage);
  };

  return (
    <SettingsSection
      title="Inventario y farmacia"
      subtitle="Avisos de vencimiento, categorías y unidades disponibles."
      err={err}
      msg={msg}
      onSave={handleSave}
      busy={busy}
    >
      {value ? (
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="field">
              <label className="label">Aviso «próximo a vencer» (días)</label>
              <NumberInput min={1} max={365} className="input" value={value.expiringSoonDays} onChange={(n) => update('expiringSoonDays', n)} />
            </div>
            <div className="field">
              <label className="label">Stock mínimo por defecto</label>
              <NumberInput min={0} className="input" value={value.defaultMinStock} onChange={(n) => update('defaultMinStock', n)} />
            </div>
          </div>

          <div className="space-y-6">
            <StringListTable
              title="Categorías"
              items={value.categories}
              onChange={(items) => update('categories', items)}
              placeholder="Nueva categoría"
              addLabel="Crear categoría"
              emptyText="Sin categorías registradas."
              extraColumns={[{ header: 'En uso', cell: (item) => <UsageBadge count={usage?.categories[item]} /> }]}
            />
            <StringListTable
              title="Unidades"
              items={value.units}
              onChange={(items) => update('units', items)}
              placeholder="Nueva unidad"
              addLabel="Crear unidad"
              emptyText="Sin unidades registradas."
              extraColumns={[{ header: 'En uso', cell: (item) => <UsageBadge count={usage?.units[item]} /> }]}
            />
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-400">Cargando…</p>
      )}
    </SettingsSection>
  );
}

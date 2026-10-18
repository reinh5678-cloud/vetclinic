import { useEffect, useState, type FormEvent } from 'react';
import { useAuth } from '../../context/AuthContext';
import { formatNumber } from '../../lib/format';
import { adjustStock, listItems, type InventoryItem } from '../../api/inventory';

export default function AdjustmentsPage() {
  const { can } = useAuth();
  const canManage = can('inventory.manage');
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [selectedItemId, setSelectedItemId] = useState<string>('');
  const [direction, setDirection] = useState<'in' | 'out'>('in');
  const [quantity, setQuantity] = useState('');
  const [description, setDescription] = useState('');
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');

  useEffect(() => {
    listItems({ includeInactive: true })
      .then(setItems)
      .catch((e) => setErr((e as Error).message));
  }, []);

  const item = items.find((i) => i.id === Number(selectedItemId)) || null;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!item) { setErr('Selecciona un artículo.'); return; }
    const qty = Number(quantity);
    if (!(qty > 0)) { setErr('La cantidad debe ser mayor a cero.'); return; }
    setErr('');
    setSaving(true);
    try {
      const signed = direction === 'in' ? qty : -qty;
      await adjustStock(item.id, { quantity: signed, description: description.trim() || 'Ajuste manual' });
      setMsg(direction === 'in' ? 'Entrada de stock registrada.' : 'Salida de stock registrada (FEFO).');
      setQuantity('');
      setDescription('');
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Ajustes de stock</h2>
          <p className="page-subtitle">Entradas o salidas manuales con aplicación FEFO para salidas.</p>
        </div>
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      <div className="card card-pad max-w-3xl">
        <form onSubmit={submit} className="space-y-3">
          <div className="field">
            <label className="label">Artículo</label>
            <select value={selectedItemId} onChange={(e) => setSelectedItemId(e.target.value)} className="select" required>
              <option value="">-- Seleccionar artículo --</option>
              {items.map((i) => (
                <option key={i.id} value={i.id}>{i.name} ({i.code}) · stock: {formatNumber(i.stockQuantity)} {i.unit}</option>
              ))}
            </select>
          </div>
          {item && (
            <p className="text-sm text-slate-500">
              Stock actual: <strong className="text-slate-800">{formatNumber(item.stockQuantity)} {item.unit}</strong>
            </p>
          )}
          <div className="field">
            <label className="label">Tipo de ajuste</label>
            <select value={direction} onChange={(e) => setDirection(e.target.value as 'in' | 'out')} className="select">
              <option value="in">Entrada (suma stock, crea lote de ajuste)</option>
              <option value="out">Salida (resta stock por FEFO)</option>
            </select>
          </div>
          <div className="field">
            <label className="label">Cantidad</label>
            <input type="number" min="0" step="0.001" value={quantity} onChange={(e) => setQuantity(e.target.value)} className="input" required />
          </div>
          <div className="field">
            <label className="label">Motivo</label>
            <input value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Ej: recuento físico, merma, consumo interno…" className="input" />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="submit" className="btn btn-primary" disabled={saving || !canManage}>
              {saving ? 'Aplicando…' : 'Aplicar ajuste'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

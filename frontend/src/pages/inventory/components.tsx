import { useState, type FormEvent } from 'react';
import Modal from '../../components/Modal';
import {
  createItem,
  updateItem,
  type InventoryItem,
  type InventoryItemInput,
} from '../../api/inventory';

const CATEGORY_SUGGESTIONS = ['Medicamentos', 'Vacunas', 'AntibiÃ³ticos', 'AnestÃ©sicos', 'Insumos', 'Alimentos', 'Otros'];

interface ItemFormModalProps {
  item: InventoryItem | null;
  categories: string[];
  onClose: () => void;
  onSaved: (msg: string) => void;
}

export function ItemFormModal({ item, categories, onClose, onSaved }: ItemFormModalProps) {
  const editing = item !== null;
  const [form, setForm] = useState({
    name: item?.name ?? '',
    category: item?.category ?? '',
    code: item?.code ?? '',
    unit: item?.unit ?? 'un',
    minStock: item ? String(item.minStock) : '10',
    unitPrice: item ? String(item.unitPrice) : '0',
    supplier: item?.supplier ?? '',
    isFractionable: item?.isFractionable ?? false,
  });
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState('');

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    if (!form.name.trim() || !form.category.trim() || !form.code.trim() || !form.unit.trim()) {
      setErr('Nombre, categorÃ­a, cÃ³digo y unidad son obligatorios.');
      return;
    }
    const payload: InventoryItemInput = {
      name: form.name.trim(),
      category: form.category.trim(),
      code: form.code.trim(),
      unit: form.unit.trim(),
      minStock: Number(form.minStock) || 0,
      unitPrice: Number(form.unitPrice) || 0,
      supplier: form.supplier.trim(),
      isFractionable: form.isFractionable,
    };
    setSaving(true);
    try {
      if (editing) {
        await updateItem(item.id, payload);
        onSaved('ArtÃ­culo actualizado.');
      } else {
        await createItem(payload);
        onSaved('ArtÃ­culo creado.');
      }
    } catch (ex) {
      setErr((ex as Error).message);
      setSaving(false);
    }
  };

  return (
    <Modal title={editing ? `Editar ${item.name}` : 'Nuevo artÃ­culo'} onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        {err && <div className="alert alert-error">{err}</div>}
        <div className="field">
          <label className="label">Nombre *</label>
          <input value={form.name} onChange={(e) => set('name', e.target.value)} className="input" />
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="field">
            <label className="label">CategorÃ­a *</label>
            <input list="inventory-categories" value={form.category} onChange={(e) => set('category', e.target.value)} className="input" />
            <datalist id="inventory-categories">
              {CATEGORY_SUGGESTIONS.map((c) => <option key={c} value={c} />)}
            </datalist>
          </div>
          <div className="field">
            <label className="label">CÃ³digo *</label>
            <input value={form.code} onChange={(e) => set('code', e.target.value)} className="input" />
          </div>
          <div className="field">
            <label className="label">Unidad *</label>
            <input value={form.unit} onChange={(e) => set('unit', e.target.value)} className="input" />
          </div>
          <div className="field">
            <label className="label">Proveedor</label>
            <input value={form.supplier} onChange={(e) => set('supplier', e.target.value)} className="input" />
          </div>
          <div className="field">
            <label className="label">Stock mÃ­nimo</label>
            <input type="number" min="0" step="0.001" value={form.minStock} onChange={(e) => set('minStock', e.target.value)} className="input" />
          </div>
          <div className="field">
            <label className="label">Precio unitario</label>
            <input type="number" min="0" step="0.01" value={form.unitPrice} onChange={(e) => set('unitPrice', e.target.value)} className="input" />
          </div>
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" className="checkbox" checked={form.isFractionable} onChange={(e) => set('isFractionable', e.target.checked)} />
          Fraccionable (permite cantidades decimales)
        </label>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" className="btn btn-secondary" onClick={onClose} disabled={saving}>Cancelar</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardandoâ€¦' : editing ? 'Guardar cambios' : 'Crear artÃ­culo'}
          </button>
        </div>
        <p className="text-xs text-slate-400">Las categorÃ­as sugeridas son orientativas; puedes escribir una nueva.</p>
        {categories.length > 0 && (
          <p className="text-xs text-slate-400">Existentes: {categories.join(', ')}</p>
        )}
      </form>
    </Modal>
  );
}

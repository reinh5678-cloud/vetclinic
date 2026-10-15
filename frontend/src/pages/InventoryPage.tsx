import { useCallback, useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import ConfirmDialog from '../components/ConfirmDialog';
import { formatCurrency, formatNumber } from '../lib/format';
import {
  addLot,
  adjustStock,
  createItem,
  deactivateLot,
  listExpiringLots,
  listItems,
  listLots,
  listMovements,
  reactivateLot,
  setItemStatus,
  updateItem,
  MOVEMENT_LABEL,
  type ExpiringLot,
  type InventoryItem,
  type InventoryItemInput,
  type ProductLot,
  type StockMovement,
} from '../api/inventory';

const CATEGORY_SUGGESTIONS = ['Medicamentos', 'Vacunas', 'Antibióticos', 'Anestésicos', 'Insumos', 'Alimentos', 'Otros'];
const DAY_OPTIONS = [7, 30, 60, 90];

type Tab = 'items' | 'expiring';

export default function InventoryPage() {
  const { can } = useAuth();
  const canManage = can('inventory.manage');
  const [searchParams, setSearchParams] = useSearchParams();

  const [tab, setTab] = useState<Tab>(() => (searchParams.get('tab') === 'lotes' ? 'expiring' : 'items'));
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [reload, setReload] = useState(0);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');

  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [lowOnly, setLowOnly] = useState(() => searchParams.get('low') === '1');
  const [includeInactive, setIncludeInactive] = useState(false);

  const [expiringDays, setExpiringDays] = useState(30);
  const [expiring, setExpiring] = useState<ExpiringLot[]>([]);
  const [expiringLoading, setExpiringLoading] = useState(false);

  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [lots, setLots] = useState<Record<number, ProductLot[]>>({});
  const [lotsIncludeInactive, setLotsIncludeInactive] = useState(false);

  const [editorItem, setEditorItem] = useState<InventoryItem | 'new' | null>(null);
  const [adjustItem, setAdjustItem] = useState<InventoryItem | null>(null);
  const [kardexItem, setKardexItem] = useState<InventoryItem | null>(null);
  const [statusTarget, setStatusTarget] = useState<InventoryItem | null>(null);
  const [lotStatusTarget, setLotStatusTarget] = useState<{ itemId: number; lot: ProductLot } | null>(null);
  const [busy, setBusy] = useState(false);

  const bump = () => setReload((r) => r + 1);
  const notify = (text: string) => { setErr(''); setMsg(text); };

  const loadCategories = useCallback(() => {
    listItems()
      .then((all) => setCategories(Array.from(new Set(all.map((i) => i.category))).sort()))
      .catch(console.error);
  }, []);

  useEffect(() => { loadCategories(); }, [loadCategories]);

  useEffect(() => {
    if (tab !== 'items') return;
    setLoading(true);
    const handle = setTimeout(() => {
      listItems({
        search: search.trim() || undefined,
        category: category || undefined,
        lowOnly,
        includeInactive,
      })
        .then(setItems)
        .catch((e) => setErr((e as Error).message))
        .finally(() => setLoading(false));
    }, 250);
    return () => clearTimeout(handle);
  }, [tab, search, category, lowOnly, includeInactive, reload]);

  useEffect(() => {
    if (tab !== 'expiring') return;
    setExpiringLoading(true);
    listExpiringLots(expiringDays)
      .then(setExpiring)
      .catch((e) => setErr((e as Error).message))
      .finally(() => setExpiringLoading(false));
  }, [tab, expiringDays, reload]);

  const loadLotsFor = useCallback((itemId: number, includeInactive: boolean) => {
    return listLots(itemId, includeInactive)
      .then((l) => setLots((prev) => ({ ...prev, [itemId]: l })))
      .catch((e) => setErr((e as Error).message));
  }, []);

  const changeTab = (next: Tab) => {
    setTab(next);
    const p = new URLSearchParams(searchParams);
    if (next === 'expiring') p.set('tab', 'lotes'); else p.delete('tab');
    setSearchParams(p, { replace: true });
  };

  const toggleLowOnly = () => {
    const next = !lowOnly;
    setLowOnly(next);
    const p = new URLSearchParams(searchParams);
    if (next) p.set('low', '1'); else p.delete('low');
    setSearchParams(p, { replace: true });
  };

  const toggleExpand = (item: InventoryItem) => {
    const next = expandedId === item.id ? null : item.id;
    setExpandedId(next);
    if (next !== null) loadLotsFor(item.id, lotsIncludeInactive);
  };

  const toggleLotsInactive = () => {
    const next = !lotsIncludeInactive;
    setLotsIncludeInactive(next);
    if (expandedId !== null) loadLotsFor(expandedId, next);
  };

  const confirmStatus = async () => {
    if (!statusTarget) return;
    setBusy(true);
    try {
      await setItemStatus(statusTarget.id, !statusTarget.isActive);
      notify(statusTarget.isActive ? 'Artículo desactivado.' : 'Artículo reactivado.');
      setStatusTarget(null);
      bump();
      loadCategories();
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const confirmLotStatus = async () => {
    if (!lotStatusTarget) return;
    const { itemId, lot } = lotStatusTarget;
    setBusy(true);
    try {
      if (lot.isActive) {
        await deactivateLot(lot.id);
        notify(`Lote ${lot.lotNumber} dado de baja.`);
      } else {
        await reactivateLot(lot.id);
        notify(`Lote ${lot.lotNumber} reactivado.`);
      }
      setLotStatusTarget(null);
      await loadLotsFor(itemId, lotsIncludeInactive);
      bump();
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setBusy(false);
    }
  };

  const activeCategories = useMemo(() => {
    const set = new Set(categories);
    items.forEach((i) => set.add(i.category));
    return Array.from(set).sort();
  }, [categories, items]);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Inventario y Farmacia</h2>
          <p className="page-subtitle">Control de stock por lote, reposición y vencimientos.</p>
        </div>
        {canManage && (
          <button type="button" className="btn btn-primary" onClick={() => setEditorItem('new')}>
            + Nuevo artículo
          </button>
        )}
      </div>

      <div className="mb-4 flex gap-2">
        <button
          type="button"
          className={`btn btn-sm ${tab === 'items' ? 'btn-primary' : 'btn-secondary'}`}
          onClick={() => changeTab('items')}
        >
          Artículos
        </button>
        <button
          type="button"
          className={`btn btn-sm ${tab === 'expiring' ? 'btn-primary' : 'btn-secondary'}`}
          onClick={() => changeTab('expiring')}
        >
          Lotes por vencer
        </button>
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {tab === 'items' && (
        <>
          <div className="card card-pad mb-4 flex flex-wrap items-end gap-3">
            <div className="field min-w-56 flex-1">
              <label className="label">Buscar</label>
              <input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Nombre o código…"
                className="input"
              />
            </div>
            <div className="field w-48">
              <label className="label">Categoría</label>
              <select value={category} onChange={(e) => setCategory(e.target.value)} className="select">
                <option value="">Todas</option>
                {activeCategories.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <label className="flex items-center gap-2 py-2 text-sm text-slate-700">
              <input type="checkbox" className="checkbox" checked={lowOnly} onChange={toggleLowOnly} />
              Solo bajo stock
            </label>
            <label className="flex items-center gap-2 py-2 text-sm text-slate-700">
              <input type="checkbox" className="checkbox" checked={includeInactive} onChange={(e) => setIncludeInactive(e.target.checked)} />
              Incluir inactivos
            </label>
          </div>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Artículo</th>
                  <th className="text-center">Categoría</th>
                  <th className="text-right">Stock</th>
                  <th className="text-right">Stock mín.</th>
                  <th className="text-right">Precio</th>
                  <th className="text-center">Fracc.</th>
                  <th className="text-center">Estado</th>
                  <th className="text-center">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {items.map((i) => (
                  <ItemRow
                    key={i.id}
                    item={i}
                    canManage={canManage}
                    expanded={expandedId === i.id}
                    onToggle={() => toggleExpand(i)}
                    onAdjust={() => setAdjustItem(i)}
                    onKardex={() => setKardexItem(i)}
                    onEdit={() => setEditorItem(i)}
                    onStatus={() => setStatusTarget(i)}
                  />
                ))}
                {!loading && items.length === 0 && (
                  <tr>
                    <td colSpan={9} className="py-10 text-center text-slate-400">
                      {lowOnly || search || category
                        ? 'No hay artículos que coincidan con los filtros.'
                        : 'Sin artículos registrados. Crea el primero para comenzar.'}
                    </td>
                  </tr>
                )}
                {loading && (
                  <tr>
                    <td colSpan={9} className="py-10 text-center text-slate-400">Cargando…</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {expandedId !== null && (
            <LotsSection
              item={items.find((i) => i.id === expandedId) ?? null}
              lots={lots[expandedId] ?? []}
              includeInactive={lotsIncludeInactive}
              onToggleInactive={toggleLotsInactive}
              canManage={canManage}
              onLotStatus={(lot) => setLotStatusTarget({ itemId: expandedId, lot })}
              onAddLot={async (input) => {
                await addLot(expandedId, input);
                notify('Lote registrado y stock actualizado.');
                await loadLotsFor(expandedId, lotsIncludeInactive);
                bump();
              }}
              onError={(text) => { setErr(text); setMsg(''); }}
            />
          )}

          <div className="alert alert-info mt-4">
            <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <p>El stock se consume por lote (FEFO, primero en vencer) al facturar o al dar de baja un uso interno. Registra un lote por compra para poder vender el artículo.</p>
          </div>
        </>
      )}

      {tab === 'expiring' && (
        <>
          <div className="card card-pad mb-4 flex flex-wrap items-end gap-3">
            <div className="field w-44">
              <label className="label">Vencen en</label>
              <select value={expiringDays} onChange={(e) => setExpiringDays(Number(e.target.value))} className="select">
                {DAY_OPTIONS.map((d) => <option key={d} value={d}>{d} días</option>)}
              </select>
            </div>
          </div>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Artículo</th>
                  <th>Código</th>
                  <th>Lote</th>
                  <th className="text-center">Vence</th>
                  <th className="text-right">Disponible</th>
                  <th className="text-center">Estado</th>
                </tr>
              </thead>
              <tbody>
                {expiring.map((l) => (
                  <tr key={l.lotId}>
                    <td className="font-medium text-slate-800">{l.itemName}</td>
                    <td>{l.itemCode}</td>
                    <td>{l.lotNumber}</td>
                    <td className="text-center">{l.expirationDate ? new Date(l.expirationDate).toLocaleDateString() : '—'}</td>
                    <td className="text-right">{formatNumber(l.availableQuantity)}</td>
                    <td className="text-center">
                      {l.isExpired
                        ? <span className="badge badge-danger">VENCIDO</span>
                        : <span className="badge badge-warning">POR VENCER</span>}
                    </td>
                  </tr>
                ))}
                {!expiringLoading && expiring.length === 0 && (
                  <tr>
                    <td colSpan={6} className="py-10 text-center text-slate-400">
                      No hay lotes por vencer en los próximos {expiringDays} días.
                    </td>
                  </tr>
                )}
                {expiringLoading && (
                  <tr>
                    <td colSpan={6} className="py-10 text-center text-slate-400">Cargando…</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}

      {editorItem !== null && (
        <ItemFormModal
          key={editorItem === 'new' ? 'new' : editorItem.id}
          item={editorItem === 'new' ? null : editorItem}
          categories={activeCategories}
          onClose={() => setEditorItem(null)}
          onSaved={(m) => { notify(m); setEditorItem(null); bump(); loadCategories(); }}
        />
      )}

      {adjustItem && (
        <AdjustStockModal
          item={adjustItem}
          onClose={() => setAdjustItem(null)}
          onSaved={(m) => { notify(m); setAdjustItem(null); bump(); if (expandedId === adjustItem.id) loadLotsFor(adjustItem.id, lotsIncludeInactive); }}
        />
      )}

      {kardexItem && (
        <KardexModal item={kardexItem} onClose={() => setKardexItem(null)} />
      )}

      {statusTarget && (
        <ConfirmDialog
          title={statusTarget.isActive ? 'Desactivar artículo' : 'Reactivar artículo'}
          danger={statusTarget.isActive}
          confirmLabel={statusTarget.isActive ? 'Desactivar' : 'Reactivar'}
          busy={busy}
          message={
            statusTarget.isActive
              ? <>El artículo <strong>{statusTarget.name}</strong> dejará de aparecer en ventas e inventario activo. Su historial de movimientos se conserva.</>
              : <>El artículo <strong>{statusTarget.name}</strong> volverá a estar disponible para ventas y uso interno.</>
          }
          onConfirm={confirmStatus}
          onCancel={() => setStatusTarget(null)}
        />
      )}

      {lotStatusTarget && (
        <ConfirmDialog
          title="Dar de baja lote"
          danger
          confirmLabel="Dar de baja"
          busy={busy}
          message={<>Se darán de baja las <strong>{formatNumber(lotStatusTarget.lot.availableQuantity)}</strong> unidades del lote <strong>{lotStatusTarget.lot.lotNumber}</strong> y se descontarán del stock. Podrás reactivarlo luego.</>}
          onConfirm={confirmLotStatus}
          onCancel={() => setLotStatusTarget(null)}
        />
      )}
    </div>
  );
}

interface ItemRowProps {
  item: InventoryItem;
  canManage: boolean;
  expanded: boolean;
  onToggle: () => void;
  onAdjust: () => void;
  onKardex: () => void;
  onEdit: () => void;
  onStatus: () => void;
}

function ItemRow({ item, canManage, expanded, onToggle, onAdjust, onKardex, onEdit, onStatus }: ItemRowProps) {
  return (
    <tr className={item.isActive ? undefined : 'text-slate-400'}>
      <td className="font-medium text-slate-800">{item.code}</td>
      <td>
        {item.name}
        {!item.isActive && <span className="badge badge-neutral ml-2">INACTIVO</span>}
      </td>
      <td className="text-center">{item.category}</td>
      <td className="text-right">{formatNumber(item.stockQuantity)} {item.unit}</td>
      <td className="text-right">{formatNumber(item.minStock)}</td>
      <td className="text-right">{formatCurrency(item.unitPrice)}</td>
      <td className="text-center">{item.isFractionable ? 'Sí' : 'No'}</td>
      <td className="text-center">
        {!item.isActive
          ? <span className="badge badge-neutral">INACTIVO</span>
          : item.lowStock
            ? <span className="badge badge-danger">REPOSICIÓN</span>
            : <span className="badge badge-success">OK</span>}
      </td>
      <td>
        <div className="flex justify-center gap-1">
          <button type="button" className="btn btn-secondary btn-xs" onClick={onToggle}>
            {expanded ? 'Ocultar' : 'Lotes'}
          </button>
          {canManage && (
            <>
              <button type="button" className="btn btn-ghost btn-xs" onClick={onAdjust}>Ajustar</button>
              <button type="button" className="btn btn-ghost btn-xs" onClick={onEdit}>Editar</button>
              <button type="button" className="btn btn-ghost btn-xs" onClick={onStatus}>
                {item.isActive ? 'Desactivar' : 'Reactivar'}
              </button>
            </>
          )}
          <button type="button" className="btn btn-ghost btn-xs" onClick={onKardex}>Kardex</button>
        </div>
      </td>
    </tr>
  );
}

interface LotsSectionProps {
  item: InventoryItem | null;
  lots: ProductLot[];
  includeInactive: boolean;
  onToggleInactive: () => void;
  canManage: boolean;
  onLotStatus: (lot: ProductLot) => void;
  onAddLot: (input: { lotNumber: string; quantity: number; expirationDate: string | null; description: string }) => Promise<void>;
  onError: (text: string) => void;
}

function LotsSection({ item, lots, includeInactive, onToggleInactive, canManage, onLotStatus, onAddLot, onError }: LotsSectionProps) {
  const [lotNumber, setLotNumber] = useState('');
  const [quantity, setQuantity] = useState('');
  const [expirationDate, setExpirationDate] = useState('');
  const [description, setDescription] = useState('');
  const [saving, setSaving] = useState(false);

  if (!item) return null;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const qty = Number(quantity);
    if (!lotNumber.trim() || !(qty > 0)) {
      onError('Indica número de lote y cantidad mayor a cero.');
      return;
    }
    setSaving(true);
    try {
      await onAddLot({
        lotNumber: lotNumber.trim(),
        quantity: qty,
        expirationDate: expirationDate ? new Date(expirationDate).toISOString() : null,
        description: description.trim(),
      });
      setLotNumber(''); setQuantity(''); setExpirationDate(''); setDescription('');
    } catch (ex) {
      onError((ex as Error).message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="card card-pad mt-4">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="section-title">Lotes de {item.name}</h3>
        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input type="checkbox" className="checkbox" checked={includeInactive} onChange={onToggleInactive} />
          Mostrar inactivos
        </label>
      </div>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(280px,340px)]">
        <div>
          {lots.length === 0 ? (
            <p className="text-sm text-slate-400">Sin lotes {includeInactive ? '' : 'activos '}registrados. Registra una entrada para habilitar la venta.</p>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Lote</th>
                    <th className="text-center">Vence</th>
                    <th className="text-right">Disponible</th>
                    <th className="text-center">Estado</th>
                    {canManage && <th className="text-center">Acción</th>}
                  </tr>
                </thead>
                <tbody>
                  {lots.map((l) => (
                    <tr key={l.id} className={l.isActive ? undefined : 'text-slate-400'}>
                      <td>{l.lotNumber}</td>
                      <td className="text-center">{l.expirationDate ? new Date(l.expirationDate).toLocaleDateString() : '—'}</td>
                      <td className="text-right">{formatNumber(l.availableQuantity)}</td>
                      <td className="text-center">
                        {l.isActive
                          ? l.isExpiringSoon
                            ? <span className="badge badge-warning">POR VENCER</span>
                            : <span className="badge badge-success">OK</span>
                          : <span className="badge badge-neutral">INACTIVO</span>}
                      </td>
                      {canManage && (
                        <td className="text-center">
                          <button type="button" className="btn btn-ghost btn-xs" onClick={() => onLotStatus(l)}>
                            {l.isActive ? 'Dar de baja' : 'Reactivar'}
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {canManage && (
          <form onSubmit={submit} className="card p-4">
            <h4 className="mb-3 text-sm font-semibold text-slate-700">Registrar entrada (lote)</h4>
            <div className="flex flex-col gap-2">
              <input placeholder="N° de lote *" value={lotNumber} onChange={(e) => setLotNumber(e.target.value)} className="input" />
              <input type="number" min="0" step="0.001" placeholder="Cantidad *" value={quantity} onChange={(e) => setQuantity(e.target.value)} className="input" />
              <label className="text-xs text-slate-500">
                Vencimiento
                <input type="date" value={expirationDate} onChange={(e) => setExpirationDate(e.target.value)} className="input mt-1" />
              </label>
              <input placeholder="Descripción (opcional)" value={description} onChange={(e) => setDescription(e.target.value)} className="input" />
              <button type="submit" className="btn btn-primary btn-sm" disabled={saving}>
                {saving ? 'Guardando…' : 'Registrar lote'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

interface ItemFormModalProps {
  item: InventoryItem | null;
  categories: string[];
  onClose: () => void;
  onSaved: (msg: string) => void;
}

function ItemFormModal({ item, categories, onClose, onSaved }: ItemFormModalProps) {
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
      setErr('Nombre, categoría, código y unidad son obligatorios.');
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
        onSaved('Artículo actualizado.');
      } else {
        await createItem(payload);
        onSaved('Artículo creado.');
      }
    } catch (ex) {
      setErr((ex as Error).message);
      setSaving(false);
    }
  };

  return (
    <Modal title={editing ? `Editar ${item.name}` : 'Nuevo artículo'} onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        {err && <div className="alert alert-error">{err}</div>}
        <div className="field">
          <label className="label">Nombre *</label>
          <input value={form.name} onChange={(e) => set('name', e.target.value)} className="input" />
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="field">
            <label className="label">Categoría *</label>
            <input list="inventory-categories" value={form.category} onChange={(e) => set('category', e.target.value)} className="input" />
            <datalist id="inventory-categories">
              {CATEGORY_SUGGESTIONS.map((c) => <option key={c} value={c} />)}
            </datalist>
          </div>
          <div className="field">
            <label className="label">Código *</label>
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
            <label className="label">Stock mínimo</label>
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
            {saving ? 'Guardando…' : editing ? 'Guardar cambios' : 'Crear artículo'}
          </button>
        </div>
        <p className="text-xs text-slate-400">Las categorías sugeridas son orientativas; puedes escribir una nueva.</p>
        {categories.length > 0 && (
          <p className="text-xs text-slate-400">Existentes: {categories.join(', ')}</p>
        )}
      </form>
    </Modal>
  );
}

interface AdjustStockModalProps {
  item: InventoryItem;
  onClose: () => void;
  onSaved: (msg: string) => void;
}

function AdjustStockModal({ item, onClose, onSaved }: AdjustStockModalProps) {
  const [direction, setDirection] = useState<'in' | 'out'>('in');
  const [quantity, setQuantity] = useState('');
  const [description, setDescription] = useState('');
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState('');

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const qty = Number(quantity);
    if (!(qty > 0)) { setErr('La cantidad debe ser mayor a cero.'); return; }
    setErr('');
    setSaving(true);
    try {
      const signed = direction === 'in' ? qty : -qty;
      await adjustStock(item.id, { quantity: signed, description: description.trim() || 'Ajuste manual' });
      onSaved(direction === 'in' ? 'Entrada de stock registrada.' : 'Salida de stock registrada (FEFO).');
    } catch (ex) {
      setErr((ex as Error).message);
      setSaving(false);
    }
  };

  return (
    <Modal title={`Ajustar stock · ${item.name}`} onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        {err && <div className="alert alert-error">{err}</div>}
        <p className="text-sm text-slate-500">
          Stock actual: <strong className="text-slate-800">{formatNumber(item.stockQuantity)} {item.unit}</strong>
        </p>
        <div className="field">
          <label className="label">Tipo de ajuste</label>
          <select value={direction} onChange={(e) => setDirection(e.target.value as 'in' | 'out')} className="select">
            <option value="in">Entrada (suma stock, crea lote de ajuste)</option>
            <option value="out">Salida (resta stock por FEFO)</option>
          </select>
        </div>
        <div className="field">
          <label className="label">Cantidad</label>
          <input type="number" min="0" step="0.001" value={quantity} onChange={(e) => setQuantity(e.target.value)} className="input" />
        </div>
        <div className="field">
          <label className="label">Motivo</label>
          <input value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Ej: recuento físico, merma, consumo interno…" className="input" />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" className="btn btn-secondary" onClick={onClose} disabled={saving}>Cancelar</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Aplicando…' : 'Aplicar ajuste'}
          </button>
        </div>
      </form>
    </Modal>
  );
}

interface KardexModalProps {
  item: InventoryItem;
  onClose: () => void;
}

function KardexModal({ item, onClose }: KardexModalProps) {
  const [movements, setMovements] = useState<StockMovement[]>([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState('');

  useEffect(() => {
    listMovements(item.id)
      .then(setMovements)
      .catch((e) => setErr((e as Error).message))
      .finally(() => setLoading(false));
  }, [item.id]);

  return (
    <Modal title={`Kardex · ${item.name}`} onClose={onClose} wide>
      {err && <div className="alert alert-error">{err}</div>}
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Movimiento</th>
              <th className="text-right">Cantidad</th>
              <th>Lote</th>
              <th>Usuario</th>
              <th>Detalle</th>
            </tr>
          </thead>
          <tbody>
            {movements.map((m) => (
              <tr key={m.id}>
                <td>{new Date(m.movementDate).toLocaleString()}</td>
                <td>{MOVEMENT_LABEL[m.type] ?? m.type}</td>
                <td className={`text-right font-medium ${m.quantity < 0 ? 'text-rose-600' : 'text-emerald-600'}`}>
                  {m.quantity > 0 ? '+' : ''}{formatNumber(m.quantity)}
                </td>
                <td>{m.lotNumber ?? '—'}</td>
                <td>{m.performedByName ?? '—'}</td>
                <td className="max-w-xs truncate" title={m.description}>{m.description}</td>
              </tr>
            ))}
            {!loading && movements.length === 0 && (
              <tr>
                <td colSpan={6} className="py-10 text-center text-slate-400">Sin movimientos registrados.</td>
              </tr>
            )}
            {loading && (
              <tr>
                <td colSpan={6} className="py-10 text-center text-slate-400">Cargando…</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </Modal>
  );
}

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  wide?: boolean;
}

function Modal({ title, onClose, children, wide }: ModalProps) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
    >
      <div className={`card my-8 w-full ${wide ? 'max-w-3xl' : 'max-w-lg'}`} onClick={(e) => e.stopPropagation()}>
        <div className="card-header flex items-center justify-between">
          <h3 className="section-title">{title}</h3>
          <button type="button" className="btn btn-ghost btn-xs" onClick={onClose}>Cerrar</button>
        </div>
        <div className="card-body">{children}</div>
      </div>
    </div>
  );
}

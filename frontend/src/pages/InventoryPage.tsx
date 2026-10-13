import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';

interface Item {
  id: number; name: string; category: string; code: string; stockQuantity: number; minStock: number; unitPrice: number; supplier: string; lowStock: boolean; isFractionable: boolean;
}
interface Lot {
  id: number; inventoryItemId: number; lotNumber: string; expirationDate: string | null; availableQuantity: number; isExpiringSoon: boolean;
}

export default function InventoryPage() {
  const [items, setItems] = useState<Item[]>([]);
  const [lots, setLots] = useState<Record<number, Lot[]>>({});
  const [name, setName] = useState('');
  const [category, setCategory] = useState('');
  const [code, setCode] = useState('');
  const [minStock, setMinStock] = useState(10);
  const [isFractionable, setIsFractionable] = useState(false);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [lotNumber, setLotNumber] = useState('');
  const [lotQty, setLotQty] = useState(0);
  const [lotExpiry, setLotExpiry] = useState('');
  const [lotDesc, setLotDesc] = useState('');
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');

  const load = () => api<Item[]>('/inventory').then(setItems).catch(console.error);
  useEffect(() => { load(); }, []);

  const loadLots = (itemId: number) =>
    api<Lot[]>(`/inventory/${itemId}/lots`).then((l) => setLots((prev) => ({ ...prev, [itemId]: l }))).catch(console.error);

  const toggleItem = (itemId: number) => {
    const next = expandedId === itemId ? null : itemId;
    setExpandedId(next);
    if (next !== null) loadLots(next);
  };

  const create = async (e: FormEvent) => {
    e.preventDefault();
    setErr('');
    try {
      await api('/inventory', {
        method: 'POST',
        body: JSON.stringify({ name, category, code, unit: 'un', minStock, unitPrice: 0, supplier: '', isFractionable }),
      });
      setName(''); setCategory(''); setCode(''); setIsFractionable(false);
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const adjust = async (id: number, delta: number) => {
    setErr('');
    try {
      await api(`/inventory/${id}/stock`, { method: 'PATCH', body: JSON.stringify({ quantity: delta, description: 'Ajuste manual' }) });
      setMsg('Stock ajustado.');
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const addLot = async (e: FormEvent) => {
    e.preventDefault();
    if (!expandedId || lotQty <= 0 || !lotNumber.trim()) { setErr('Indica número de lote y cantidad mayor a cero.'); return; }
    setErr('');
    try {
      await api(`/inventory/${expandedId}/lots`, {
        method: 'POST',
        body: JSON.stringify({
          lotNumber,
          quantity: lotQty,
          expirationDate: lotExpiry ? new Date(lotExpiry).toISOString() : null,
          description: lotDesc || 'Entrada por compra',
        }),
      });
      setLotNumber(''); setLotQty(0); setLotExpiry(''); setLotDesc('');
      setMsg('Lote registrado y stock actualizado.');
      await loadLots(expandedId);
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const deactivateLot = async (lotId: number) => {
    if (!expandedId) return;
    await api(`/inventory/lots/${lotId}`, { method: 'DELETE' });
    await loadLots(expandedId);
    load();
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Inventario y Farmacia</h2>
          <p className="page-subtitle">Control de stock por lote, reposición y vencimientos.</p>
        </div>
      </div>

      <form onSubmit={create} className="card card-pad flex flex-wrap items-end gap-3">
        <div className="field min-w-40 flex-1">
          <label className="label">Nombre</label>
          <input placeholder="Nombre" value={name} onChange={(e) => setName(e.target.value)} className="input" />
        </div>
        <div className="field min-w-36">
          <label className="label">Categoría</label>
          <input placeholder="Categoría" value={category} onChange={(e) => setCategory(e.target.value)} className="input" />
        </div>
        <div className="field min-w-32">
          <label className="label">Código</label>
          <input placeholder="Código" value={code} onChange={(e) => setCode(e.target.value)} className="input" />
        </div>
        <div className="field w-28">
          <label className="label">Stock mín.</label>
          <input type="number" value={minStock} onChange={(e) => setMinStock(Number(e.target.value))} className="input" />
        </div>
        <label className="flex items-center gap-2 py-2 text-sm text-slate-700">
          <input type="checkbox" className="checkbox" checked={isFractionable} onChange={(e) => setIsFractionable(e.target.checked)} /> Fraccionable
        </label>
        <button type="submit" className="btn btn-primary">Agregar</button>
      </form>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Código</th>
              <th>Artículo</th>
              <th className="text-center">Categoría</th>
              <th className="text-center">Stock</th>
              <th className="text-center">Stock mín.</th>
              <th className="text-center">Fracc.</th>
              <th className="text-center">Estado</th>
              <th className="text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {items.map((i) => (
              <ItemRows
                key={i.id}
                item={i}
                expanded={expandedId === i.id}
                onToggle={() => toggleItem(i.id)}
                onAdjust={adjust}
                lots={lots[i.id] ?? []}
                lotNumber={lotNumber}
                setLotNumber={setLotNumber}
                lotQty={lotQty}
                setLotQty={setLotQty}
                lotExpiry={lotExpiry}
                setLotExpiry={setLotExpiry}
                lotDesc={lotDesc}
                setLotDesc={setLotDesc}
                onAddLot={addLot}
                onDeactivateLot={deactivateLot}
              />
            ))}
          </tbody>
        </table>
      </div>

      <div className="alert alert-info">
        <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>Rol requerido: Veterinario o Administrador. El stock se consume por lote (FEFO) al facturar. Registra un lote (EntradaCompra) para poder vender el artículo.</p>
      </div>
    </div>
  );
}

interface ItemRowsProps {
  item: Item;
  expanded: boolean;
  onToggle: () => void;
  onAdjust: (id: number, delta: number) => void;
  lots: Lot[];
  lotNumber: string;
  setLotNumber: (v: string) => void;
  lotQty: number;
  setLotQty: (v: number) => void;
  lotExpiry: string;
  setLotExpiry: (v: string) => void;
  lotDesc: string;
  setLotDesc: (v: string) => void;
  onAddLot: (e: FormEvent) => void;
  onDeactivateLot: (lotId: number) => void;
}

function ItemRows(props: ItemRowsProps) {
  const { item } = props;
  const [delta, setDelta] = useState(0);
  return (
    <>
      <tr>
        <td className="font-medium text-slate-800">{item.code}</td>
        <td>{item.name}</td>
        <td className="text-center">{item.category}</td>
        <td className="text-center font-medium text-slate-800">{item.stockQuantity}</td>
        <td className="text-center">{item.minStock}</td>
        <td className="text-center">{item.isFractionable ? 'Sí' : 'No'}</td>
        <td className="text-center">
          {item.lowStock
            ? <span className="badge badge-danger">REPOSICIÓN</span>
            : <span className="badge badge-success">OK</span>}
        </td>
        <td className="text-center">
          <button onClick={() => props.onToggle()} className="btn btn-secondary btn-xs">
            {props.expanded ? 'Ocultar lotes' : 'Ver lotes'}
          </button>
        </td>
      </tr>
      {props.expanded && (
        <tr className="hover:bg-transparent">
          <td colSpan={9} className="bg-slate-50/70 p-4">
            <div className="grid gap-4 lg:grid-cols-[minmax(240px,1fr)_minmax(280px,1.4fr)_minmax(260px,1fr)]">
              <section className="card p-4">
                <h4 className="mb-3 text-sm font-semibold text-slate-700">Ajuste manual de stock</h4>
                <div className="flex gap-2">
                  <input
                    type="number"
                    step="0.001"
                    value={delta}
                    onChange={(e) => setDelta(Number(e.target.value))}
                    className="input w-28"
                    aria-label={`Ajuste de stock para ${item.name}`}
                  />
                  <button
                    type="button"
                    disabled={delta === 0}
                    onClick={() => { props.onAdjust(item.id, delta); setDelta(0); }}
                    className="btn btn-primary btn-sm"
                  >Ajustar</button>
                </div>
                <p className="mt-2 text-xs text-slate-400">Positivo = entrada (crea lote de ajuste). Negativo = salida (consumo FEFO).</p>
              </section>

              <section>
                <h4 className="mb-3 text-sm font-semibold text-slate-700">Lotes ({props.lots.length})</h4>
                {props.lots.length === 0 ? (
                  <p className="text-sm text-slate-400">Sin lotes activos. Registra un lote para habilitar la venta.</p>
                ) : (
                  <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-slate-200 bg-slate-100">
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Lote</th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Vence</th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Disponible</th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Estado</th>
                          <th></th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {props.lots.map((l) => (
                          <tr key={l.id} className="hover:bg-slate-50/70">
                            <td className="px-3 py-2 text-slate-600">{l.lotNumber}</td>
                            <td className="px-3 py-2 text-slate-600">{l.expirationDate ? new Date(l.expirationDate).toLocaleDateString() : '—'}</td>
                            <td className="px-3 py-2 text-slate-600">{l.availableQuantity}</td>
                            <td className="px-3 py-2">
                              {l.isExpiringSoon
                                ? <span className="badge badge-warning">PRÓX. A VENCER</span>
                                : <span className="badge badge-success">OK</span>}
                            </td>
                            <td className="px-3 py-2 text-right">
                              <button type="button" className="btn btn-ghost btn-xs" onClick={() => props.onDeactivateLot(l.id)}>Desactivar</button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>

              <section className="card p-4">
                <h4 className="mb-3 text-sm font-semibold text-slate-700">Registrar lote (entrada por compra)</h4>
                <form onSubmit={props.onAddLot} className="flex flex-col gap-2">
                  <input placeholder="N° de lote *" value={props.lotNumber} onChange={(e) => props.setLotNumber(e.target.value)} className="input" required />
                  <input type="number" min="0.001" step="0.001" placeholder="Cantidad *" value={props.lotQty} onChange={(e) => props.setLotQty(Number(e.target.value))} className="input" required />
                  <label className="text-xs text-slate-500">
                    Vencimiento
                    <input type="date" value={props.lotExpiry} onChange={(e) => props.setLotExpiry(e.target.value)} className="input mt-1" />
                  </label>
                  <input placeholder="Descripción (opcional)" value={props.lotDesc} onChange={(e) => props.setLotDesc(e.target.value)} className="input" />
                  <button type="submit" className="btn btn-primary btn-sm">Registrar lote</button>
                </form>
              </section>
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

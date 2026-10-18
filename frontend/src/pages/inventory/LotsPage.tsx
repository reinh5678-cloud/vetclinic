import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import ConfirmDialog from '../../components/ConfirmDialog';
import { formatNumber } from '../../lib/format';
import {
  addLot,
  deactivateLot,
  listExpiringLots,
  listItems,
  listLots,
  reactivateLot,
  type ExpiringLot,
  type InventoryItem,
  type ProductLot,
} from '../../api/inventory';

const DAY_OPTIONS = [7, 30, 60, 90];

type Tab = 'all' | 'expiring';

export default function LotsPage() {
  const { can } = useAuth();
  const canManage = can('inventory.manage');

  const [tab, setTab] = useState<Tab>('all');
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [selectedItemId, setSelectedItemId] = useState<string>('');
  const [lots, setLots] = useState<ProductLot[]>([]);
  const [includeInactive, setIncludeInactive] = useState(false);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState('');
  const [msg, setMsg] = useState('');

  const [expiringDays, setExpiringDays] = useState(30);
  const [expiring, setExpiring] = useState<ExpiringLot[]>([]);
  const [expiringLoading, setExpiringLoading] = useState(false);

  const [lotNumber, setLotNumber] = useState('');
  const [quantity, setQuantity] = useState('');
  const [expirationDate, setExpirationDate] = useState('');
  const [description, setDescription] = useState('');
  const [saving, setSaving] = useState(false);

  const [lotStatusTarget, setLotStatusTarget] = useState<ProductLot | null>(null);
  const [busy, setBusy] = useState(false);

  const notify = (text: string) => { setErr(''); setMsg(text); };

  useEffect(() => {
    listItems({ includeInactive: true })
      .then(setItems)
      .catch((e) => setErr((e as Error).message));
  }, []);

  const loadLots = useCallback(() => {
    if (!selectedItemId) {
      setLots([]);
      return;
    }
    setLoading(true);
    listLots(Number(selectedItemId), includeInactive)
      .then(setLots)
      .catch((e) => setErr((e as Error).message))
      .finally(() => setLoading(false));
  }, [selectedItemId, includeInactive]);

  useEffect(() => {
    if (tab === 'all') loadLots();
  }, [tab, selectedItemId, includeInactive, loadLots]);

  useEffect(() => {
    if (tab !== 'expiring') return;
    setExpiringLoading(true);
    listExpiringLots(expiringDays)
      .then(setExpiring)
      .catch((e) => setErr((e as Error).message))
      .finally(() => setExpiringLoading(false));
  }, [tab, expiringDays]);

  const submitLot = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedItemId) { setErr('Selecciona un artículo.'); return; }
    const qty = Number(quantity);
    if (!lotNumber.trim() || !(qty > 0)) { setErr('Indica número de lote y cantidad mayor a cero.'); return; }
    setErr('');
    setSaving(true);
    try {
      await addLot(Number(selectedItemId), {
        lotNumber: lotNumber.trim(),
        quantity: qty,
        expirationDate: expirationDate ? new Date(expirationDate).toISOString() : null,
        description: description.trim(),
      });
      notify('Lote registrado y stock actualizado.');
      setLotNumber(''); setQuantity(''); setExpirationDate(''); setDescription('');
      loadLots();
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const confirmLotStatus = async () => {
    if (!lotStatusTarget || !selectedItemId) return;
    setBusy(true);
    try {
      if (lotStatusTarget.isActive) {
        await deactivateLot(lotStatusTarget.id);
        notify('Lote dado de baja.');
      } else {
        await reactivateLot(lotStatusTarget.id);
        notify('Lote reactivado.');
      }
      setLotStatusTarget(null);
      loadLots();
    } catch (ex) {
      setErr((ex as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Lotes</h2>
          <p className="page-subtitle">Gestión de lotes por artículo y vencimientos.</p>
        </div>
      </div>

      <div className="mb-4 flex gap-2">
        <button type="button" className="btn btn-sm" onClick={() => setTab('all')}>
          Por artículo
        </button>
        <button type="button" className="btn btn-sm" onClick={() => setTab('expiring')}>
          Lotes por vencer
        </button>
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {tab === 'all' && (
        <>
          <div className="card card-pad mb-4 flex flex-wrap items-end gap-3">
            <div className="field min-w-56 flex-1">
              <label className="label">Artículo</label>
              <select value={selectedItemId} onChange={(e) => setSelectedItemId(e.target.value)} className="select">
                <option value="">-- Seleccionar artículo --</option>
                {items.map((i) => (
                  <option key={i.id} value={i.id}>{i.name} ({i.code})</option>
                ))}
              </select>
            </div>
            <label className="flex items-center gap-2 text-sm text-slate-600">
              <input type="checkbox" className="checkbox" checked={includeInactive} onChange={(e) => setIncludeInactive(e.target.checked)} />
              Mostrar inactivos
            </label>
          </div>

          {canManage && selectedItemId && (
            <div className="card card-pad mb-4">
              <h3 className="section-title mb-3">Registrar entrada (lote)</h3>
              <form onSubmit={submitLot} className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                <input placeholder="N° de lote *" value={lotNumber} onChange={(e) => setLotNumber(e.target.value)} className="input" />
                <input type="number" min="0" step="0.001" placeholder="Cantidad *" value={quantity} onChange={(e) => setQuantity(e.target.value)} className="input" />
                <input type="date" value={expirationDate} onChange={(e) => setExpirationDate(e.target.value)} className="input" />
                <input placeholder="Descripción (opcional)" value={description} onChange={(e) => setDescription(e.target.value)} className="input" />
                <div className="sm:col-span-2 lg:col-span-4">
                  <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Guardando…' : 'Registrar lote'}</button>
                </div>
              </form>
            </div>
          )}

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
                        <button type="button" className="btn btn-ghost btn-xs" onClick={() => setLotStatusTarget(l)}>
                          {l.isActive ? 'Dar de baja' : 'Reactivar'}
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
                {!loading && lots.length === 0 && (
                  <tr>
                    <td colSpan={canManage ? 5 : 4} className="py-10 text-center text-slate-400">
                      {selectedItemId ? 'Sin lotes registrados.' : 'Selecciona un artículo para ver sus lotes.'}
                    </td>
                  </tr>
                )}
                {loading && (
                  <tr>
                    <td colSpan={canManage ? 5 : 4} className="py-10 text-center text-slate-400">Cargando…</td>
                  </tr>
                )}
              </tbody>
            </table>
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

      {lotStatusTarget && (
        <ConfirmDialog
          title={lotStatusTarget.isActive ? 'Dar de baja lote' : 'Reactivar lote'}
          danger={lotStatusTarget.isActive}
          confirmLabel={lotStatusTarget.isActive ? 'Dar de baja' : 'Reactivar'}
          busy={busy}
          message={<>
            {lotStatusTarget.isActive
              ? <>Se darán de baja las <strong>{formatNumber(lotStatusTarget.availableQuantity)}</strong> unidades del lote <strong>{lotStatusTarget.lotNumber}</strong> y se descontarán del stock.</>
              : <>Se reactivarán las <strong>{formatNumber(lotStatusTarget.availableQuantity)}</strong> unidades del lote <strong>{lotStatusTarget.lotNumber}</strong> en el stock.</>}
          </>}
          onConfirm={confirmLotStatus}
          onCancel={() => setLotStatusTarget(null)}
        />
      )}
    </div>
  );
}





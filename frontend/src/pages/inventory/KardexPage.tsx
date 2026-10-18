import { useCallback, useEffect, useMemo, useState } from 'react';
import Pagination from '../../components/Pagination';
import { formatNumber } from '../../lib/format';
import {
  listItems,
  listKardex,
  MOVEMENT_LABEL,
  type InventoryItem,
  type KardexEntry,
  type StockMovementType,
} from '../../api/inventory';

const PAGE_SIZE = 15;

const TYPE_OPTIONS: StockMovementType[] = ['EntradaCompra', 'SalidaVenta', 'SalidaUsoInterno', 'Ajuste'];

export default function KardexPage() {
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [rows, setRows] = useState<KardexEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState('');

  const [itemId, setItemId] = useState('');
  const [type, setType] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    listItems({ includeInactive: true })
      .then(setItems)
      .catch((e) => setErr((e as Error).message));
  }, []);

  useEffect(() => {
    setLoading(true);
    const handle = setTimeout(() => {
      listKardex({
        itemId: itemId ? Number(itemId) : undefined,
        type: (type || undefined) as StockMovementType | undefined,
        from: from || undefined,
        to: to || undefined,
      })
        .then(setRows)
        .catch((e) => setErr((e as Error).message))
        .finally(() => setLoading(false));
    }, 250);
    return () => clearTimeout(handle);
  }, [itemId, type, from, to]);

  useEffect(() => { setPage(1); }, [itemId, type, from, to]);

  const clearFilters = useCallback(() => {
    setItemId('');
    setType('');
    setFrom('');
    setTo('');
  }, []);

  const hasFilters = Boolean(itemId || type || from || to);
  const totalPages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pagedRows = rows.slice((currentPage - 1) * PAGE_SIZE, (currentPage - 1) * PAGE_SIZE + PAGE_SIZE);

  const totals = useMemo(() => {
    const entries = rows.filter((r) => r.quantity > 0).reduce((sum, r) => sum + r.quantity, 0);
    const exits = rows.filter((r) => r.quantity < 0).reduce((sum, r) => sum + r.quantity, 0);
    return { entries, exits, net: entries + exits };
  }, [rows]);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Kardex</h2>
          <p className="page-subtitle">Historial de movimientos de stock de todos los artículos.</p>
        </div>
      </div>

      {err && <div className="alert alert-error">{err}</div>}

      <div className="card card-pad mb-4 flex flex-wrap items-end gap-3">
        <div className="field min-w-56 flex-1">
          <label className="label">Artículo</label>
          <select value={itemId} onChange={(e) => setItemId(e.target.value)} className="select">
            <option value="">Todos los artículos</option>
            {items.map((i) => <option key={i.id} value={i.id}>{i.name} ({i.code})</option>)}
          </select>
        </div>
        <div className="field w-52">
          <label className="label">Movimiento</label>
          <select value={type} onChange={(e) => setType(e.target.value)} className="select">
            <option value="">Todos</option>
            {TYPE_OPTIONS.map((t) => <option key={t} value={t}>{MOVEMENT_LABEL[t]}</option>)}
          </select>
        </div>
        <div className="field w-40">
          <label className="label">Desde</label>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="input" />
        </div>
        <div className="field w-40">
          <label className="label">Hasta</label>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="input" />
        </div>
        {hasFilters && (
          <button type="button" className="btn btn-secondary" onClick={clearFilters}>Limpiar</button>
        )}
      </div>

      {rows.length > 0 && (
        <div className="card card-pad mb-4 grid gap-3 sm:grid-cols-3">
          <div>
            <p className="text-xs text-slate-500">Entradas</p>
            <p className="text-lg font-semibold text-emerald-600">+{formatNumber(totals.entries)}</p>
          </div>
          <div>
            <p className="text-xs text-slate-500">Salidas</p>
            <p className="text-lg font-semibold text-rose-600">{formatNumber(totals.exits)}</p>
          </div>
          <div>
            <p className="text-xs text-slate-500">Neto</p>
            <p className={`text-lg font-semibold ${totals.net >= 0 ? 'text-emerald-600' : 'text-rose-600'}`}>
              {totals.net > 0 ? '+' : ''}{formatNumber(totals.net)}
            </p>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Artículo</th>
              <th className="text-center">Movimiento</th>
              <th className="text-right">Cantidad</th>
              <th>Lote</th>
              <th>Usuario</th>
              <th>Detalle</th>
            </tr>
          </thead>
          <tbody>
            {pagedRows.map((m) => (
              <tr key={m.id}>
                <td>{new Date(m.movementDate).toLocaleString()}</td>
                <td>
                  <span className="font-medium text-slate-800">{m.itemName}</span>
                  <span className="ml-2 text-xs text-slate-400">{m.itemCode}</span>
                </td>
                <td className="text-center">
                  <span className={`badge ${m.quantity < 0 ? 'badge-danger' : 'badge-success'}`}>
                    {MOVEMENT_LABEL[m.type] ?? m.type}
                  </span>
                </td>
                <td className={`text-right font-medium ${m.quantity < 0 ? 'text-rose-600' : 'text-emerald-600'}`}>
                  {m.quantity > 0 ? '+' : ''}{formatNumber(m.quantity)}
                </td>
                <td>{m.lotNumber ?? '—'}</td>
                <td>{m.performedByName ?? '—'}</td>
                <td className="group max-w-xs">
                  <div className="cell-expand">{m.description}</div>
                </td>
              </tr>
            ))}
            {!loading && rows.length === 0 && (
              <tr>
                <td colSpan={7} className="py-10 text-center text-slate-400">
                  {hasFilters ? 'No hay movimientos que coincidan con los filtros.' : 'Sin movimientos registrados.'}
                </td>
              </tr>
            )}
            {loading && (
              <tr>
                <td colSpan={7} className="py-10 text-center text-slate-400">Cargando…</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={currentPage} totalItems={rows.length} pageSize={PAGE_SIZE} onPageChange={setPage} />

      {rows.length >= 200 && (
        <p className="mt-3 text-xs text-slate-400">
          Se muestran los 200 movimientos más recientes. Acota el rango de fechas o el artículo para ver el resto.
        </p>
      )}
    </div>
  );
}
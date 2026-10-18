import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import ConfirmDialog from '../../components/ConfirmDialog';
import Pagination from '../../components/Pagination';
import { formatCurrency, formatNumber } from '../../lib/format';
import { listItems, setItemStatus, type InventoryItem } from '../../api/inventory';
import { ItemFormModal } from './components';

const PAGE_SIZE = 10;

export default function InventoryArticlesPage() {
  const { can } = useAuth();
  const canManage = can('inventory.manage');
  const [searchParams, setSearchParams] = useSearchParams();

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
  const [page, setPage] = useState(1);

  const [editorItem, setEditorItem] = useState<InventoryItem | 'new' | null>(null);
  const [statusTarget, setStatusTarget] = useState<InventoryItem | null>(null);
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
  }, [search, category, lowOnly, includeInactive, reload]);

  useEffect(() => { setPage(1); }, [search, category, lowOnly, includeInactive]);

  const totalPages = Math.max(1, Math.ceil(items.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageStart = (currentPage - 1) * PAGE_SIZE;
  const pagedItems = items.slice(pageStart, pageStart + PAGE_SIZE);

  const toggleLowOnly = () => {
    const next = !lowOnly;
    setLowOnly(next);
    const p = new URLSearchParams(searchParams);
    if (next) p.set('low', '1'); else p.delete('low');
    setSearchParams(p, { replace: true });
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

  const activeCategories = useMemo(() => {
    const set = new Set(categories);
    items.forEach((i) => set.add(i.category));
    return Array.from(set).sort();
  }, [categories, items]);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Artículos de inventario</h2>
          <p className="page-subtitle">Gestión de artículos, stock y lotes.</p>
        </div>
        {canManage && (
          <button type="button" className="btn btn-primary" onClick={() => setEditorItem('new')}>
            + Nuevo artículo
          </button>
        )}
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      <div className="card card-pad mb-4 flex flex-wrap items-end gap-3">
        <div className="field min-w-56 flex-1">
          <label className="label">Buscar</label>
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Nombre o código…" className="input" />
        </div>
        <div className="field w-48">
          <label className="label">Categoría</label>
          <select value={category} onChange={(e) => setCategory(e.target.value)} className="select">
            <option value="">Todas</option>
            {activeCategories.map((c) => <option key={c} value={c}>{c}</option>)}
          </select>
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" className="checkbox" checked={lowOnly} onChange={toggleLowOnly} />
          Solo bajo stock
        </label>
        <label className="flex items-center gap-2 text-sm text-slate-700">
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
            {pagedItems.map((i) => (
              <ItemRow
                key={i.id}
                item={i}
                canManage={canManage}
                onEdit={() => setEditorItem(i)}
                onStatus={() => setStatusTarget(i)}
              />
            ))}
            {!loading && items.length === 0 && (
              <tr>
                <td colSpan={9} className="py-10 text-center text-slate-400">
                  {lowOnly || search || category ? 'No hay artículos que coincidan con los filtros.' : 'Sin artículos registrados.'}
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

      <Pagination page={currentPage} totalItems={items.length} pageSize={PAGE_SIZE} onPageChange={setPage} />

      <div className="alert alert-info mt-4">
        <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>El stock se consume por lote (FEFO, primero en vencer) al facturar o al dar de baja un uso interno. Registra las compras en la pestaña <strong>Lotes</strong> y las correcciones de stock en <strong>Ajustes</strong>; sin lote disponible el artículo no se puede vender.</p>
      </div>

      {editorItem !== null && (
        <ItemFormModal
          key={editorItem === 'new' ? 'new' : editorItem.id}
          item={editorItem === 'new' ? null : editorItem}
          categories={activeCategories}
          onClose={() => setEditorItem(null)}
          onSaved={(m) => { notify(m); setEditorItem(null); bump(); loadCategories(); }}
        />
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
    </div>
  );
}

interface ItemRowProps {
  item: InventoryItem;
  canManage: boolean;
  onEdit: () => void;
  onStatus: () => void;
}

function ItemRow({ item, canManage, onEdit, onStatus }: ItemRowProps) {
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
        {canManage ? (
          <div className="flex justify-center gap-1">
            <button type="button" className="btn btn-ghost btn-xs" onClick={onEdit}>Editar</button>
            <button type="button" className="btn btn-ghost btn-xs" onClick={onStatus}>
              {item.isActive ? 'Desactivar' : 'Reactivar'}
            </button>
          </div>
        ) : (
          <span className="text-xs text-slate-400">—</span>
        )}
      </td>
    </tr>
  );
}

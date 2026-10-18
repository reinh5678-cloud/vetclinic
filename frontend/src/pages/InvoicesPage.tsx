import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { listItems, type InventoryItem } from '../api/inventory';
import Modal from '../components/Modal';
import Pagination from '../components/Pagination';
import { formatCurrency } from '../lib/format';

interface Invoice {
  id: number; invoiceNumber: string; issueDate: string; status: string; subtotal: number; taxes: number; total: number; paymentMethod: string; tutorName: string | null;
}
interface Tutor {
  id: number; firstName: string; lastName: string;
}
interface Pet {
  id: number; name: string;
}

const STATUSES = ['Borrador', 'Emitida', 'Pagada', 'Anulada'];
const PAYMENTS = ['Efectivo', 'Tarjeta', 'Transferencia'];

const transitions: Record<string, string[]> = {
  Borrador: ['Emitida', 'Anulada'],
  Emitida: ['Pagada', 'Anulada'],
  Pagada: [],
  Anulada: [],
};

const statusBadge: Record<string, string> = {
  Borrador: 'badge-neutral',
  Emitida: 'badge-info',
  Pagada: 'badge-success',
  Anulada: 'badge-danger',
};
const PAGE_SIZE = 10;

export default function InvoicesPage() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [tutors, setTutors] = useState<Tutor[]>([]);
  const [pets, setPets] = useState<Pet[]>([]);
  const [inventoryItems, setInventoryItems] = useState<InventoryItem[]>([]);
  const [tutorId, setTutorId] = useState<number | ''>('');
  const [petId, setPetId] = useState<number | ''>('');
  const [inventoryItemId, setInventoryItemId] = useState<number | ''>('');
  const [status, setStatus] = useState('Emitida');
  const [paymentMethod, setPaymentMethod] = useState('Efectivo');
  const [description, setDescription] = useState('Consulta general');
  const [quantity, setQuantity] = useState(1);
  const [unitPrice, setUnitPrice] = useState(50);
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [page, setPage] = useState(1);
  const [statusTarget, setStatusTarget] = useState<Invoice | null>(null);
  const [showNew, setShowNew] = useState(false);

  const load = () => {
    setPage(1);
    api<Invoice[]>('/invoices').then(setInvoices).catch(console.error);
  };
  useEffect(() => {
    load();
    api<Tutor[]>('/patients/tutors').then(setTutors).catch(console.error);
    api<Pet[]>('/patients/pets').then(setPets).catch(console.error);
    listItems({ includeInactive: false }).then(setInventoryItems).catch(console.error);
  }, []);

  const selectedItem = useMemo(
    () => inventoryItems.find((i) => i.id === inventoryItemId) ?? null,
    [inventoryItems, inventoryItemId],
  );

  const totalPages = Math.max(1, Math.ceil(invoices.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageStart = (currentPage - 1) * PAGE_SIZE;
  const pagedInvoices = invoices.slice(pageStart, pageStart + PAGE_SIZE);

  const pickInventoryItem = (value: string) => {
    if (value === '') {
      setInventoryItemId('');
      return;
    }
    const id = Number(value);
    const item = inventoryItems.find((i) => i.id === id);
    setInventoryItemId(id);
    if (item) {
      setDescription(item.name);
      setUnitPrice(item.unitPrice);
    }
  };

  const openNew = () => { setErr(''); setMsg(''); setShowNew(true); };
  const closeNew = () => { setShowNew(false); };

  const create = async (e: FormEvent) => {
    e.preventDefault();
    if (tutorId === '' && petId === '') { setErr('Selecciona un tutor o una mascota.'); return; }
    setErr('');
    setMsg('');
    try {
      await api('/invoices', {
        method: 'POST',
        body: JSON.stringify({
          tutorId: tutorId === '' ? null : Number(tutorId),
          petId: petId === '' ? null : Number(petId),
          status,
          paymentMethod,
          lines: [{
            description,
            quantity,
            unitPrice,
            inventoryItemId: inventoryItemId === '' ? null : inventoryItemId,
          }],
        }),
      });
      setMsg('Factura emitida.');
      closeNew();
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  const updateStatus = async (id: number, status: string) => {
    setErr('');
    try {
      await api(`/invoices/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) });
      setMsg(`Factura actualizada a «${status}».`);
      load();
    } catch (ex) {
      setErr((ex as Error).message);
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Facturación</h2>
          <p className="page-subtitle">Emisión de comprobantes y control de estados de pago.</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={openNew}>+ Nueva factura</button>
      </div>

      {err && <div className="alert alert-error">{err}</div>}
      {msg && <div className="alert alert-success">{msg}</div>}

      {showNew && (
        <Modal title="Nueva factura" onClose={closeNew}>
          <form onSubmit={create} className="space-y-3">
            {err && <div className="alert alert-error">{err}</div>}
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="field">
                <label className="label">Tutor</label>
                <select value={tutorId} onChange={(e) => setTutorId(e.target.value === '' ? '' : Number(e.target.value))} className="select">
                  <option value="">Tutor…</option>
                  {tutors.map((t) => (
                    <option key={t.id} value={t.id}>{t.firstName} {t.lastName}</option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label className="label">Mascota</label>
                <select value={petId} onChange={(e) => setPetId(e.target.value === '' ? '' : Number(e.target.value))} className="select">
                  <option value="">Mascota…</option>
                  {pets.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label className="label">Estado</label>
                <select value={status} onChange={(e) => setStatus(e.target.value)} className="select">
                  {STATUSES.map((s) => <option key={s}>{s}</option>)}
                </select>
              </div>
              <div className="field">
                <label className="label">Pago</label>
                <select value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)} className="select">
                  {PAYMENTS.map((p) => <option key={p}>{p}</option>)}
                </select>
              </div>
            </div>
            <div className="field">
              <label className="label">Artículo (opcional)</label>
              <select value={inventoryItemId} onChange={(e) => pickInventoryItem(e.target.value)} className="select">
                <option value="">Sin descuento de stock</option>
                {inventoryItems.map((i) => (
                  <option key={i.id} value={i.id}>{i.name} · stock {i.stockQuantity} {i.unit}</option>
                ))}
              </select>
            </div>
            <div className="field">
              <label className="label">Descripción</label>
              <input value={description} onChange={(e) => setDescription(e.target.value)} className="input" />
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="field">
                <label className="label">Cantidad</label>
                <input type="number" min="0" step="0.001" value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} className="input" />
              </div>
              <div className="field">
                <label className="label">Precio</label>
                <input type="number" step="0.01" value={unitPrice} onChange={(e) => setUnitPrice(Number(e.target.value))} className="input" />
              </div>
            </div>
            {selectedItem && (
              <div className="alert alert-info">
                <p>
                  Al emitir se descontará stock de <strong>{selectedItem.name}</strong> por lote en orden FEFO.
                  {selectedItem.stockQuantity < quantity && <span className="ml-1 text-rose-600">Stock insuficiente: disponible {selectedItem.stockQuantity} {selectedItem.unit}.</span>}
                </p>
              </div>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn btn-secondary" onClick={closeNew}>Cancelar</button>
              <button type="submit" className="btn btn-primary">Emitir factura</button>
            </div>
          </form>
        </Modal>
      )}

      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>N°</th>
              <th>Fecha</th>
              <th>Cliente</th>
              <th className="text-center">Subtotal</th>
              <th className="text-center">IGV</th>
              <th className="text-center">Total</th>
              <th className="text-center">Estado</th>
              <th className="text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {pagedInvoices.map((i) => (
              <tr key={i.id}>
                <td className="font-medium text-slate-800">{i.invoiceNumber}</td>
                <td className="whitespace-nowrap">{new Date(i.issueDate).toLocaleDateString()}</td>
                <td>{i.tutorName ?? '-'}</td>
                <td className="text-right">{formatCurrency(i.subtotal)}</td>
                <td className="text-right">{formatCurrency(i.taxes)}</td>
                <td className="text-right font-bold text-slate-800">{formatCurrency(i.total)}</td>
                <td className="text-center">
                  <span className={`badge ${statusBadge[i.status] ?? 'badge-neutral'}`}>{i.status}</span>
                </td>
                <td className="text-center">
                  {transitions[i.status]?.length > 0 && (
                    <button type="button" className="btn btn-secondary btn-xs" onClick={() => setStatusTarget(i)}>
                      Cambiar estado
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {invoices.length === 0 && (
              <tr>
                <td colSpan={8} className="py-10 text-center text-slate-400">No hay facturas registradas.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={currentPage} totalItems={invoices.length} pageSize={PAGE_SIZE} onPageChange={setPage} />

      <div className="alert alert-info">
        <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>El backend calcula subtotal, IGV y total automáticamente según la configuración vigente.</p>
      </div>

      {statusTarget && (
        <Modal title={`Cambiar estado · ${statusTarget.invoiceNumber}`} onClose={() => setStatusTarget(null)}>
          <div className="space-y-3">
            <p className="text-sm text-slate-600">
              Estado actual: <span className="font-medium text-slate-800">{statusTarget.status}</span>
            </p>
            <div className="flex flex-wrap gap-2">
              {(transitions[statusTarget.status] ?? []).map((s) => (
                <button
                  key={s}
                  type="button"
                  className="btn btn-primary btn-sm"
                  onClick={() => { const target = statusTarget; setStatusTarget(null); updateStatus(target.id, s); }}
                >
                  Marcar como {s}
                </button>
              ))}
            </div>
            <div className="flex justify-end">
              <button type="button" className="btn btn-secondary" onClick={() => setStatusTarget(null)}>Cancelar</button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

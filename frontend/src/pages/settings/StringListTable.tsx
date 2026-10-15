import { useState, type ReactNode } from 'react';

export interface ExtraColumn {
  header: string;
  cell: (item: string, index: number) => ReactNode;
}

interface Props {
  title: string;
  items: string[];
  onChange: (items: string[]) => void;
  placeholder?: string;
  addLabel?: string;
  emptyText?: string;
  pageSize?: number;
  extraColumns?: ExtraColumn[];
}

function pageNumbers(current: number, total: number): (number | '…')[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const pages = new Set<number>([1, total, current, current - 1, current + 1]);
  const sorted = [...pages].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const result: (number | '…')[] = [];
  let prev = 0;
  for (const p of sorted) {
    if (prev && p - prev > 1) result.push('…');
    result.push(p);
    prev = p;
  }
  return result;
}

export default function StringListTable({ title, items, onChange, placeholder, addLabel = 'Agregar', emptyText = 'Sin elementos.', pageSize = 10, extraColumns = [] }: Props) {
  const [draft, setDraft] = useState('');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editingValue, setEditingValue] = useState('');
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);

  const totalPages = Math.max(1, Math.ceil(items.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const start = (currentPage - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  const exists = (name: string, ignoreIndex = -1) =>
    items.some((it, i) => i !== ignoreIndex && it.toLowerCase() === name.toLowerCase());

  const add = () => {
    const name = draft.trim();
    if (!name) return;
    if (exists(name)) {
      setError(`«${name}» ya existe.`);
      return;
    }
    onChange([...items, name]);
    setDraft('');
    setError('');
    setPage(Math.ceil((items.length + 1) / pageSize));
  };

  const startEdit = (index: number) => {
    setEditingIndex(index);
    setEditingValue(items[index]);
    setError('');
  };

  const cancelEdit = () => {
    setEditingIndex(null);
    setEditingValue('');
    setError('');
  };

  const saveEdit = () => {
    if (editingIndex === null) return;
    const name = editingValue.trim();
    if (!name) {
      setError('El nombre no puede estar vacío.');
      return;
    }
    if (exists(name, editingIndex)) {
      setError(`«${name}» ya existe.`);
      return;
    }
    onChange(items.map((it, i) => (i === editingIndex ? name : it)));
    cancelEdit();
  };

  const remove = (index: number) => {
    onChange(items.filter((_, i) => i !== index));
    if (editingIndex === index) cancelEdit();
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="section-title">{title}</h4>
        <span className="badge badge-neutral">{items.length}</span>
      </div>

      <div className="flex gap-2">
        <input
          className="input"
          placeholder={placeholder}
          value={draft}
          onChange={(e) => {
            setDraft(e.target.value);
            setError('');
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              add();
            }
          }}
        />
        <button type="button" className="btn btn-primary btn-sm" onClick={add}>
          + {addLabel}
        </button>
      </div>

      {error && <p className="text-xs text-rose-600">{error}</p>}

      {items.length === 0 ? (
        <p className="text-sm text-slate-400">{emptyText}</p>
      ) : (
        <>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Nombre</th>
                  {extraColumns.map((col) => (
                    <th key={col.header}>{col.header}</th>
                  ))}
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((item, offset) => {
                  const index = start + offset;
                  return (
                    <tr key={`${item}-${index}`}>
                      <td>
                        {editingIndex === index ? (
                          <input
                            className="input py-1"
                            value={editingValue}
                            autoFocus
                            onChange={(e) => {
                              setEditingValue(e.target.value);
                              setError('');
                            }}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') {
                                e.preventDefault();
                                saveEdit();
                              }
                              if (e.key === 'Escape') cancelEdit();
                            }}
                          />
                        ) : (
                          <span className="text-slate-800">{item}</span>
                        )}
                      </td>
                      {extraColumns.map((col) => (
                        <td key={col.header}>{col.cell(item, index)}</td>
                      ))}
                      <td>
                        <div className="flex gap-2">
                          {editingIndex === index ? (
                            <>
                              <button type="button" className="btn btn-primary btn-xs" onClick={saveEdit}>
                                Guardar
                              </button>
                              <button type="button" className="btn btn-secondary btn-xs" onClick={cancelEdit}>
                                Cancelar
                              </button>
                            </>
                          ) : (
                            <>
                              <button type="button" className="btn btn-ghost btn-xs" onClick={() => startEdit(index)}>
                                Editar
                              </button>
                              <button
                                type="button"
                                className="btn btn-ghost btn-xs text-rose-600 hover:bg-rose-50"
                                onClick={() => remove(index)}
                              >
                                Eliminar
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-xs text-slate-500">
                Mostrando {start + 1}–{Math.min(start + pageSize, items.length)} de {items.length}
              </p>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  className="btn btn-secondary btn-xs"
                  disabled={currentPage === 1}
                  onClick={() => setPage(currentPage - 1)}
                >
                  Anterior
                </button>
                {pageNumbers(currentPage, totalPages).map((p, i) =>
                  p === '…' ? (
                    <span key={`gap-${i}`} className="px-1 text-xs text-slate-400">
                      …
                    </span>
                  ) : (
                    <button
                      key={p}
                      type="button"
                      className={`btn btn-xs ${p === currentPage ? 'btn-primary' : 'btn-secondary'}`}
                      onClick={() => setPage(p)}
                    >
                      {p}
                    </button>
                  ),
                )}
                <button
                  type="button"
                  className="btn btn-secondary btn-xs"
                  disabled={currentPage === totalPages}
                  onClick={() => setPage(currentPage + 1)}
                >
                  Siguiente
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

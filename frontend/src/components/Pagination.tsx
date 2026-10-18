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

interface Props {
  page: number;
  totalItems: number;
  pageSize?: number;
  onPageChange: (page: number) => void;
}

export default function Pagination({ page, totalItems, pageSize = 10, onPageChange }: Props) {
  if (totalItems === 0) return null;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const currentPage = Math.min(page, totalPages);
  const start = (currentPage - 1) * pageSize;
  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <p className="text-xs text-slate-500">
        Mostrando {start + 1}–{Math.min(start + pageSize, totalItems)} de {totalItems}
      </p>
      {totalPages > 1 && (
        <div className="flex items-center gap-1">
          <button
            type="button"
            className="btn btn-secondary btn-xs"
            disabled={currentPage === 1}
            onClick={() => onPageChange(currentPage - 1)}
          >
            Anterior
          </button>
          {pageNumbers(currentPage, totalPages).map((p, i) =>
            p === '…' ? (
              <span key={`gap-${i}`} className="px-1 text-xs text-slate-400">…</span>
            ) : (
              <button
                key={p}
                type="button"
                className={`btn btn-xs ${p === currentPage ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => onPageChange(p)}
              >
                {p}
              </button>
            ),
          )}
          <button
            type="button"
            className="btn btn-secondary btn-xs"
            disabled={currentPage === totalPages}
            onClick={() => onPageChange(currentPage + 1)}
          >
            Siguiente
          </button>
        </div>
      )}
    </div>
  );
}

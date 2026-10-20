import { formatNumber } from '../../lib/format';

export interface MonthBarPoint {
  label: string;
  value: number;
}

type MonthBarTone = 'teal' | 'blue';

interface MonthBarsProps {
  points: MonthBarPoint[];
  /** Descripción para lectores de pantalla, p. ej. "Facturas pagadas por mes". */
  description: string;
  height?: number;
  tone?: MonthBarTone;
  emptyText?: string;
}

// Clases literales: Tailwind solo detecta las que aparecen en el código.
const tones: Record<MonthBarTone, string> = {
  teal: 'from-teal-600 to-teal-400',
  blue: 'from-blue-600 to-blue-400',
};

/**
 * Barras para magnitudes contables por mes (conteos). Se usan barras y no líneas
 * porque cada mes es un valor discreto: una línea sugeriría continuidad entre
 * fracciones de hecho, que no existen en un conteo.
 */
export default function MonthBars({
  points,
  description,
  height = 180,
  tone = 'blue',
  emptyText = 'Sin datos en el período.',
}: MonthBarsProps) {
  if (points.length === 0) {
    return (
      <div className="flex h-40 items-center justify-center text-sm text-slate-400">
        {emptyText}
      </div>
    );
  }

  const max = Math.max(...points.map((p) => p.value), 1);
  const total = points.reduce((sum, p) => sum + p.value, 0);

  return (
    <div className="w-full">
      <div className="flex items-end gap-2" style={{ height }}>
        {points.map((p, i) => {
          const pct = p.value > 0 ? Math.max((p.value / max) * 100, 3) : 0;
          return (
            <div key={`${p.label}-${i}`} className="flex min-w-0 flex-1 flex-col items-center gap-1">
              <span className="text-[10px] font-medium text-slate-400">
                {p.value > 0 ? formatNumber(p.value) : ''}
              </span>
              <div
                className="flex w-full items-end"
                style={{ height: height - 34 }}
                title={`${p.label}: ${formatNumber(p.value)}`}
              >
                <div
                  className={`w-full rounded-t-md bg-gradient-to-t ${tones[tone]} transition-all`}
                  style={{ height: `${pct}%` }}
                  aria-label={`${p.label}: ${p.value}`}
                />
              </div>
              <span className="w-full truncate text-center text-[10px] capitalize text-slate-500">{p.label}</span>
            </div>
          );
        })}
      </div>
      <div className="sr-only">
        {description}: {points.map((p) => `${p.label} ${p.value}`).join(', ')}. Total {total}.
      </div>
    </div>
  );
}
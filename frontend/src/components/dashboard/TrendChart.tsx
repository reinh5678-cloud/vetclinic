import { formatCurrency } from '../../lib/format';

interface TrendPoint {
  label: string;
  value: number;
}

interface TrendChartProps {
  points: TrendPoint[];
  height?: number;
}

export default function TrendChart({ points, height = 180 }: TrendChartProps) {
  if (points.length === 0) {
    return (
      <div className="flex h-40 items-center justify-center text-sm text-slate-400">
        Sin datos en el período.
      </div>
    );
  }

  const max = Math.max(...points.map((p) => p.value), 1);

  return (
    <div className="w-full">
      <div className="flex items-end gap-2" style={{ height }}>
        {points.map((p, i) => {
          const pct = Math.max((p.value / max) * 100, p.value > 0 ? 3 : 0);
          return (
            <div key={`${p.label}-${i}`} className="flex min-w-0 flex-1 flex-col items-center gap-1">
              <span className="text-[10px] font-medium text-slate-400">
                {p.value > 0 ? formatCurrency(p.value) : ''}
              </span>
              <div
                className="flex w-full items-end"
                style={{ height: height - 34 }}
                title={`${p.label}: ${formatCurrency(p.value)}`}
              >
                <div
                  className="w-full rounded-t-md bg-gradient-to-t from-teal-600 to-teal-400 transition-all"
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
        Ingresos por mes: {points.map((p) => `${p.label} ${p.value}`).join(', ')}.
      </div>
    </div>
  );
}

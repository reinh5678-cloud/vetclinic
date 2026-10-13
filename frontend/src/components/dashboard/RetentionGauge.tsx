interface RetentionGaugeProps {
  rate: number;
  returning: number;
  total: number;
}

export default function RetentionGauge({ rate, returning, total }: RetentionGaugeProps) {
  const size = 140;
  const stroke = 14;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const clamped = Math.max(0, Math.min(100, rate));
  const dash = (clamped / 100) * circumference;

  return (
    <div className="flex items-center gap-5">
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} className="flex-shrink-0 -rotate-90">
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="currentColor"
          className="text-slate-100"
          strokeWidth={stroke}
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="currentColor"
          className="text-teal-600 transition-all"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={`${dash} ${circumference - dash}`}
        />
      </svg>
      <div>
        <div className="text-3xl font-bold tracking-tight text-slate-800">{rate.toLocaleString('es-AR')}%</div>
        <div className="mt-1 text-sm text-slate-500">Retención de clientes</div>
        <div className="mt-1 text-xs text-slate-400">
          {returning} de {total} clientes recurrentes
        </div>
      </div>
    </div>
  );
}

import type { ReactNode } from 'react';

export type StatTone = 'teal' | 'blue' | 'indigo' | 'purple' | 'amber' | 'rose' | 'emerald';

const tones: Record<StatTone, string> = {
  teal: 'bg-teal-50 text-teal-600',
  blue: 'bg-blue-50 text-blue-600',
  indigo: 'bg-indigo-50 text-indigo-600',
  purple: 'bg-purple-50 text-purple-600',
  amber: 'bg-amber-50 text-amber-600',
  rose: 'bg-rose-50 text-rose-600',
  emerald: 'bg-emerald-50 text-emerald-600',
};

export interface StatCardProps {
  label: string;
  value: ReactNode;
  icon: ReactNode;
  tone?: StatTone;
  hint?: ReactNode;
  onNavigate?: () => void;
}

export default function StatCard({ label, value, icon, tone = 'teal', hint, onNavigate }: StatCardProps) {
  const interactive = Boolean(onNavigate);

  return (
    <div
      role={interactive ? 'button' : undefined}
      tabIndex={interactive ? 0 : undefined}
      onClick={onNavigate}
      onKeyDown={
        interactive
          ? (e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onNavigate?.();
              }
            }
          : undefined
      }
      className={[
        'card flex items-start gap-4 p-6 transition-shadow hover:shadow-[var(--shadow-pop)]',
        interactive ? 'cursor-pointer' : '',
      ].join(' ')}
    >
      <div className={`flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl ${tones[tone]}`}>
        {icon}
      </div>
      <div className="min-w-0">
        <div className="text-sm text-slate-500">{label}</div>
        <div className="mt-1 text-3xl font-bold tracking-tight text-slate-800">{value}</div>
        {hint && <div className="mt-1 text-xs text-slate-400">{hint}</div>}
      </div>
    </div>
  );
}

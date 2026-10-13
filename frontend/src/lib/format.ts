export function formatCurrency(value: number): string {
  const n = new Intl.NumberFormat('es-AR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
  return `$${n}`;
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat('es-AR').format(value);
}

export function formatPercent(value: number): string {
  return `${value.toLocaleString('es-AR', { maximumFractionDigits: 1 })}%`;
}

export function formatMonth(year: number, month: number): string {
  return new Date(year, month - 1, 1)
    .toLocaleDateString('es-AR', { month: 'short' })
    .replace('.', '');
}

export function formatTime(date: Date): string {
  return date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
}

import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
  getDashboard,
  getLowStock,
  getOperations,
  getRetention,
  getRevenues,
  type Dashboard,
  type LowStockItem,
  type Operations,
  type Retention,
  type RevenueReport,
} from '../api/reports';
import { formatCurrency, formatMonth, formatNumber, formatTime } from '../lib/format';
import StatCard, { type StatCardProps } from '../components/dashboard/StatCard';
import TrendChart from '../components/dashboard/TrendChart';
import RetentionGauge from '../components/dashboard/RetentionGauge';
import { Icons } from '../components/dashboard/icons';

type Preset = 'today' | '7d' | '30d' | 'month' | 'year' | 'custom';

const presetLabels: Record<Preset, string> = {
  today: 'Hoy',
  '7d': 'Últimos 7 días',
  '30d': 'Últimos 30 días',
  month: 'Este mes',
  year: 'Este año',
  custom: 'Personalizado',
};

function startOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function endOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(23, 59, 59, 999);
  return x;
}

function computeRange(preset: Preset, customFrom: string, customTo: string): { from: Date; to: Date } {
  const now = new Date();
  switch (preset) {
    case 'today':
      return { from: startOfDay(now), to: endOfDay(now) };
    case '7d': {
      const f = startOfDay(now);
      f.setDate(f.getDate() - 6);
      return { from: f, to: endOfDay(now) };
    }
    case 'month':
      return { from: startOfDay(new Date(now.getFullYear(), now.getMonth(), 1)), to: endOfDay(now) };
    case 'year':
      return { from: startOfDay(new Date(now.getFullYear(), 0, 1)), to: endOfDay(now) };
    case 'custom': {
      const f = customFrom ? startOfDay(new Date(customFrom)) : startOfDay(now);
      const t = customTo ? endOfDay(new Date(customTo)) : endOfDay(now);
      return { from: f, to: t };
    }
    case '30d':
    default: {
      const f = startOfDay(now);
      f.setDate(f.getDate() - 29);
      return { from: f, to: endOfDay(now) };
    }
  }
}

function CardSkeleton() {
  return (
    <div className="card flex animate-pulse items-start gap-4 p-6" aria-busy="true" aria-label="Cargando indicador">
      <div className="h-12 w-12 flex-shrink-0 rounded-xl bg-slate-100" />
      <div className="flex-1 space-y-2 py-1">
        <div className="h-3 w-24 rounded bg-slate-100" />
        <div className="h-6 w-16 rounded bg-slate-100" />
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const { user, can } = useAuth();
  const navigate = useNavigate();

  const canReports = can('reports.view');
  const canInventory = can('inventory.view');
  const canMedical = can('medical.view');
  const canHospitalization = can('hospitalization.view');
  const canAppointments = can('appointments.view');
  const canInvoices = can('invoices.view');

  const [operations, setOperations] = useState<Operations | null>(null);
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [revenues, setRevenues] = useState<RevenueReport | null>(null);
  const [retention, setRetention] = useState<Retention | null>(null);
  const [lowStock, setLowStock] = useState<LowStockItem[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const [preset, setPreset] = useState<Preset>('30d');
  const [customFrom, setCustomFrom] = useState('');
  const [customTo, setCustomTo] = useState('');

  const range = useMemo(() => computeRange(preset, customFrom, customTo), [preset, customFrom, customTo]);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const tasks: Promise<unknown>[] = [getOperations().then(setOperations)];

      if (canReports) {
        const now = new Date();
        const trendFrom = startOfDay(new Date(now.getFullYear(), now.getMonth() - 11, 1));
        const trendTo = endOfDay(now);
        tasks.push(getDashboard(range.from.toISOString(), range.to.toISOString()).then(setDashboard));
        tasks.push(getRevenues(trendFrom.toISOString(), trendTo.toISOString()).then(setRevenues));
        tasks.push(getRetention().then(setRetention));
      }
      if (canInventory) {
        tasks.push(getLowStock().then(setLowStock));
      }

      await Promise.all(tasks);
      setLastUpdated(new Date());
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, [canReports, canInventory, range]);

  useEffect(() => {
    load();
  }, [load]);

  const operationalCards = useMemo(() => {
    const cards: StatCardProps[] = [];
    if (operations && canAppointments) {
      cards.push({ label: 'Citas hoy', value: formatNumber(operations.appointmentsToday), icon: Icons.calendar, tone: 'amber', onNavigate: () => navigate('/agenda') });
      cards.push({ label: 'Citas pendientes', value: formatNumber(operations.appointmentsPending), icon: Icons.clock, tone: 'blue' });
      cards.push({ label: 'En espera / consulta', value: formatNumber(operations.appointmentsInProgress), icon: Icons.clock, tone: 'indigo' });
      cards.push({ label: 'Urgencias abiertas', value: formatNumber(operations.urgentOpen), icon: Icons.urgent, tone: 'rose' });
    }
    if (operations && canHospitalization) {
      cards.push({ label: 'Hospitalizados activos', value: formatNumber(operations.activeHospitalizations), icon: Icons.hospital, tone: 'purple', onNavigate: () => navigate('/hospitalizacion') });
    }
    if (operations && canMedical) {
      cards.push({ label: 'Vacunas próximas (30 d)', value: formatNumber(operations.upcomingVaccines), icon: Icons.vaccine, tone: 'teal', onNavigate: () => navigate('/historia') });
      cards.push({ label: 'Recordatorios pendientes', value: formatNumber(operations.pendingReminders), icon: Icons.reminder, tone: 'blue' });
    }
    if (operations && canInvoices) {
      cards.push({ label: 'Facturas sin cobrar', value: formatNumber(operations.unpaidInvoicesCount), hint: formatCurrency(operations.unpaidInvoicesAmount), icon: Icons.money, tone: 'emerald', onNavigate: () => navigate('/facturacion') });
    }
    return cards;
  }, [operations, canAppointments, canHospitalization, canMedical, canInvoices, navigate]);

  const financialCards = useMemo(() => {
    if (!dashboard) return [];
    const delta = dashboard.revenueDeltaPct;
    const deltaHint: ReactNode =
      delta == null
        ? 'Sin período anterior comparable'
        : `${delta >= 0 ? '▲' : '▼'} ${Math.abs(delta).toLocaleString('es-AR')}% vs período anterior`;
    const cards: StatCardProps[] = [
      { label: 'Ingresos (pagadas)', value: formatCurrency(dashboard.revenue), icon: Icons.revenue, tone: 'teal', hint: deltaHint },
      { label: 'Facturas (pagadas)', value: formatNumber(dashboard.invoicesCount), icon: Icons.invoice, tone: 'blue', onNavigate: () => navigate('/facturacion') },
      { label: 'Pacientes', value: formatNumber(dashboard.petsTotal), icon: Icons.pets, tone: 'indigo', onNavigate: () => navigate('/pacientes') },
      { label: 'Clientes', value: formatNumber(dashboard.clientsTotal), icon: Icons.clients, tone: 'purple' },
      { label: 'Citas programadas (período)', value: formatNumber(dashboard.appointmentsScheduled), icon: Icons.calendar, tone: 'amber', onNavigate: () => navigate('/agenda') },
      { label: 'Stock bajo', value: formatNumber(dashboard.lowStockItems), icon: Icons.warning, tone: 'rose', onNavigate: () => navigate('/inventario') },
    ];
    return cards;
  }, [dashboard, navigate]);

  const trendPoints = useMemo(
    () => (revenues?.byMonth ?? []).map((p) => ({ label: formatMonth(p.year, p.month), value: p.total })),
    [revenues],
  );

  const quickActions = useMemo(() => {
    const actions: { label: string; to: string; icon: ReactNode }[] = [];
    if (can('appointments.manage')) actions.push({ label: 'Nueva cita', to: '/agenda', icon: Icons.calendar });
    if (can('patients.manage')) actions.push({ label: 'Nuevo paciente', to: '/pacientes', icon: Icons.pets });
    if (can('invoices.manage')) actions.push({ label: 'Nueva factura', to: '/facturacion', icon: Icons.invoice });
    if (can('medical.manage')) actions.push({ label: 'Registrar consulta', to: '/historia', icon: Icons.vaccine });
    return actions;
  }, [can]);

  const firstName = user?.fullName?.split(' ')[0] ?? '';

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Hola{firstName ? `, ${firstName}` : ''}</h2>
          <p className="page-subtitle">
            {user?.role ? `Resumen de ${user.role}` : 'Resumen general'}
            {lastUpdated ? ` · actualizado ${formatTime(lastUpdated)}` : ''}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {canReports && (
            <select
              className="select w-auto"
              value={preset}
              onChange={(e) => setPreset(e.target.value as Preset)}
              aria-label="Período"
            >
              {(Object.keys(presetLabels) as Preset[]).map((p) => (
                <option key={p} value={p}>{presetLabels[p]}</option>
              ))}
            </select>
          )}
          {canReports && preset === 'custom' && (
            <div className="flex items-center gap-2">
              <input type="date" className="input w-auto" value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} aria-label="Desde" />
              <span className="text-slate-400">–</span>
              <input type="date" className="input w-auto" value={customTo} onChange={(e) => setCustomTo(e.target.value)} aria-label="Hasta" />
            </div>
          )}
          <button className="btn btn-secondary btn-sm" onClick={load} disabled={loading}>
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            Actualizar
          </button>
        </div>
      </div>

      {error && (
        <div className="alert alert-error">
          <span className="flex-1">No se pudo cargar el dashboard: {error}</span>
          <button className="btn btn-danger btn-sm" onClick={load}>Reintentar</button>
        </div>
      )}

      {quickActions.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {quickActions.map((a) => (
            <button key={a.to} className="btn btn-secondary btn-sm" onClick={() => navigate(a.to)}>
              {a.icon}
              {a.label}
            </button>
          ))}
        </div>
      )}

      <section className="space-y-3">
        <h3 className="section-title">Hoy en la clínica</h3>
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {!operations && loading && Array.from({ length: 4 }).map((_, i) => <CardSkeleton key={i} />)}
          {operationalCards.map((c) => <StatCard key={c.label} {...c} />)}
        </div>
      </section>

      {canReports && (
        <section className="space-y-3">
          <h3 className="section-title">Finanzas · {presetLabels[preset].toLowerCase()}</h3>
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {!dashboard && loading && Array.from({ length: 6 }).map((_, i) => <CardSkeleton key={i} />)}
            {financialCards.map((c) => <StatCard key={c.label} {...c} />)}
          </div>

          <div className="grid grid-cols-1 gap-5 lg:grid-cols-3">
            <div className="card card-pad lg:col-span-2">
              <div className="mb-4 flex items-center justify-between">
                <h4 className="section-title">Ingresos últimos 12 meses</h4>
                <span className="text-sm text-slate-400">Total: {formatCurrency(revenues?.total ?? 0)}</span>
              </div>
              {revenues ? <TrendChart points={trendPoints} /> : <div className="h-40 animate-pulse rounded-xl bg-slate-100" />}
            </div>
            <div className="card card-pad">
              <h4 className="section-title mb-4">Retención</h4>
              {retention ? (
                <RetentionGauge rate={retention.retentionRate} returning={retention.returning} total={retention.totalClients} />
              ) : (
                <div className="h-40 animate-pulse rounded-xl bg-slate-100" />
              )}
            </div>
          </div>
        </section>
      )}

      {canInventory && (
        <section className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="section-title">Stock bajo</h3>
            <button className="btn btn-ghost btn-sm" onClick={() => navigate('/inventario')}>Ver inventario</button>
          </div>
          {!lowStock ? (
            <div className="h-24 animate-pulse rounded-2xl bg-slate-100" />
          ) : lowStock.length === 0 ? (
            <div className="alert alert-success">Sin artículos por debajo del stock mínimo.</div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Artículo</th>
                    <th>Categoría</th>
                    <th>Código</th>
                    <th>Stock</th>
                    <th>Mínimo</th>
                  </tr>
                </thead>
                <tbody>
                  {lowStock.map((item) => (
                    <tr key={item.id}>
                      <td className="font-medium text-slate-700">{item.name}</td>
                      <td>{item.category}</td>
                      <td>{item.code}</td>
                      <td>
                        <span className="badge badge-danger">{formatNumber(item.stockQuantity)} {item.unit}</span>
                      </td>
                      <td>{formatNumber(item.minStock)} {item.unit}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      {canReports && (
        <div className="alert alert-info">
          <svg className="mt-0.5 h-5 w-5 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p>Los indicadores financieros respetan el período seleccionado. La tendencia muestra siempre los últimos 12 meses.</p>
        </div>
      )}
    </div>
  );
}

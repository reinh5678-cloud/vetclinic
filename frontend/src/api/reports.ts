import { api } from './client';

export interface Period {
  from: string;
  to: string;
}

export interface Dashboard {
  period: Period;
  revenue: number;
  previousRevenue: number;
  revenueDeltaPct: number | null;
  invoicesCount: number;
  petsTotal: number;
  clientsTotal: number;
  appointmentsScheduled: number;
  lowStockItems: number;
}

export interface Operations {
  appointmentsToday: number;
  appointmentsPending: number;
  appointmentsInProgress: number;
  urgentOpen: number;
  activeHospitalizations: number;
  upcomingVaccines: number;
  pendingReminders: number;
  unpaidInvoicesCount: number;
  unpaidInvoicesAmount: number;
}

export interface RevenuePoint {
  year: number;
  month: number;
  total: number;
}

export interface RevenueReport {
  total: number;
  byMonth: RevenuePoint[];
}

export interface Retention {
  totalClients: number;
  returning: number;
  retentionRate: number;
}

export interface LowStockItem {
  id: number;
  name: string;
  category: string;
  code: string;
  unit: string;
  stockQuantity: number;
  minStock: number;
  supplier: string;
}

function rangeQuery(from: string, to: string): string {
  return `?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`;
}

export function getDashboard(from: string, to: string): Promise<Dashboard> {
  return api<Dashboard>(`/reports/dashboard${rangeQuery(from, to)}`);
}

export function getRevenues(from: string, to: string): Promise<RevenueReport> {
  return api<RevenueReport>(`/reports/revenues${rangeQuery(from, to)}`);
}

export function getRetention(): Promise<Retention> {
  return api<Retention>('/reports/retention');
}

export function getOperations(): Promise<Operations> {
  return api<Operations>('/reports/operations');
}

export function getLowStock(): Promise<LowStockItem[]> {
  return api<LowStockItem[]>('/reports/low-stock');
}

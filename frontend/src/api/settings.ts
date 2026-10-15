import { api } from './client';

export interface ClinicSettings {
  name: string;
  legalName: string;
  taxId: string;
  taxIdType: string;
  address: string;
  phone: string;
  email: string;
  logoDataUrl: string | null;
  invoiceHeader: string;
  invoiceFooter: string;
}

export interface BillingSettings {
  taxRate: number;
  currency: string;
  currencySymbol: string;
  invoicePrefix: string;
  paymentMethods: string[];
  legalText: string;
}

export interface AgendaSettings {
  openingTime: string;
  closingTime: string;
  slotMinutes: number;
  defaultDurationMinutes: number;
  maxAdvanceDays: number;
  closedWeekdays: string[];
}

export interface InventorySettings {
  expiringSoonDays: number;
  categories: string[];
  units: string[];
  defaultMinStock: number;
}

export interface SecuritySettings {
  passwordMinLength: number;
  requireLetter: boolean;
  requireDigit: boolean;
  pinLength: number;
  sessionHours: number;
  maxFailedAttempts: number;
}

export interface InventoryUsage {
  categories: Record<string, number>;
  units: Record<string, number>;
}

export interface SystemInfo {
  environment: string;
  version: string;
  database: string;
  attachmentsPath: string;
  maxAttachmentMb: number;
}

export interface PublicBranding {
  name: string;
  logoDataUrl: string | null;
}

export const getPublicBranding = () => api<PublicBranding>('/settings/public');

export const getClinic = () => api<ClinicSettings>('/settings/clinic');
export const saveClinic = (body: ClinicSettings) =>
  api<ClinicSettings>('/settings/clinic', { method: 'PUT', body: JSON.stringify(body) });

export const getBilling = () => api<BillingSettings>('/settings/billing');
export const saveBilling = (body: BillingSettings) =>
  api<BillingSettings>('/settings/billing', { method: 'PUT', body: JSON.stringify(body) });

export const getAgenda = () => api<AgendaSettings>('/settings/agenda');
export const saveAgenda = (body: AgendaSettings) =>
  api<AgendaSettings>('/settings/agenda', { method: 'PUT', body: JSON.stringify(body) });

export const getInventory = () => api<InventorySettings>('/settings/inventory');
export const saveInventory = (body: InventorySettings) =>
  api<InventorySettings>('/settings/inventory', { method: 'PUT', body: JSON.stringify(body) });

export const getInventoryUsage = () => api<InventoryUsage>('/settings/inventory/usage');

export const getSecurity = () => api<SecuritySettings>('/settings/security');
export const saveSecurity = (body: SecuritySettings) =>
  api<SecuritySettings>('/settings/security', { method: 'PUT', body: JSON.stringify(body) });

export const getSystemInfo = () => api<SystemInfo>('/settings/system');

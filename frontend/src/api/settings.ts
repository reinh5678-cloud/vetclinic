import { api } from './client';

export interface ClinicSettings {
  name: string;
  legalName: string;
  taxId: string;
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

export interface NotificationSettings {
  enabled: boolean;
  smtpHost: string;
  smtpPort: number;
  smtpUser: string;
  smtpPasswordSet: boolean;
  useTls: boolean;
  fromName: string;
  fromEmail: string;
  channels: string[];
  vaccineReminderDaysBefore: number;
}

export interface UpdateNotificationsInput {
  enabled: boolean;
  smtpHost: string;
  smtpPort: number;
  smtpUser: string;
  smtpPassword?: string | null;
  useTls: boolean;
  fromName: string;
  fromEmail: string;
  channels: string[];
  vaccineReminderDaysBefore: number;
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

export const getSecurity = () => api<SecuritySettings>('/settings/security');
export const saveSecurity = (body: SecuritySettings) =>
  api<SecuritySettings>('/settings/security', { method: 'PUT', body: JSON.stringify(body) });

export const getNotifications = () => api<NotificationSettings>('/settings/notifications');
export const saveNotifications = (body: UpdateNotificationsInput) =>
  api<NotificationSettings>('/settings/notifications', { method: 'PUT', body: JSON.stringify(body) });
export const testNotifications = () =>
  api<{ ok: boolean; message: string }>('/settings/notifications/test', { method: 'POST' });

export const getSystemInfo = () => api<SystemInfo>('/settings/system');

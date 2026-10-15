import { api } from './client';
import type { AgendaSettings } from './settings';

export type UrgencyLevel = 'Baja' | 'Media' | 'Alta' | 'Emergencia';
export const URGENCIES: UrgencyLevel[] = ['Baja', 'Media', 'Alta', 'Emergencia'];

export type AppointmentStatus =
  | 'Programada'
  | 'EnEspera'
  | 'EnConsulta'
  | 'Completada'
  | 'Cancelada'
  | 'NoAsistio';

export const STATUSES: AppointmentStatus[] = [
  'Programada',
  'EnEspera',
  'EnConsulta',
  'Completada',
  'Cancelada',
  'NoAsistio',
];

export const STATUS_TRANSITIONS: Record<AppointmentStatus, AppointmentStatus[]> = {
  Programada: ['EnEspera', 'EnConsulta', 'Completada', 'Cancelada', 'NoAsistio'],
  EnEspera: ['EnConsulta', 'Completada', 'Cancelada', 'NoAsistio'],
  EnConsulta: ['Completada', 'Cancelada'],
  Completada: [],
  Cancelada: [],
  NoAsistio: ['Programada'],
};

export const TERMINAL_STATUSES: AppointmentStatus[] = ['Completada', 'Cancelada'];

export const ACTIVE_STATUSES: AppointmentStatus[] = ['Programada', 'EnEspera', 'EnConsulta'];

export const STATUS_BADGE: Record<string, string> = {
  Programada: 'badge-info',
  EnEspera: 'badge-warning',
  EnConsulta: 'badge-info',
  Completada: 'badge-success',
  Cancelada: 'badge-neutral',
  NoAsistio: 'badge-danger',
};

export interface Appointment {
  id: number;
  startTime: string;
  durationMinutes: number;
  urgency: string;
  status: AppointmentStatus;
  reason: string;
  notes: string;
  petId: number | null;
  petName: string | null;
  vetId: number | null;
  vetName: string | null;
}

export interface AppointmentInput {
  startTime: string;
  durationMinutes: number;
  urgency: UrgencyLevel;
  reason: string;
  petId: number;
  vetId: number;
}

export interface AppointmentFilters {
  from?: string;
  to?: string;
  vetId?: number;
  status?: string;
  petId?: number;
}

function query(params: Record<string, string | number | undefined>): string {
  const qs = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== '') qs.set(k, String(v));
  });
  const s = qs.toString();
  return s ? `?${s}` : '';
}

export const listAppointments = (filters: AppointmentFilters = {}) =>
  api<Appointment[]>(
    `/appointments${query({
      from: filters.from,
      to: filters.to,
      vetId: filters.vetId,
      status: filters.status,
      petId: filters.petId,
    })}`,
  );

export const createAppointment = (input: AppointmentInput) =>
  api<Appointment>('/appointments', { method: 'POST', body: JSON.stringify(input) });

export const updateAppointment = (id: number, input: AppointmentInput) =>
  api<Appointment>(`/appointments/${id}`, { method: 'PUT', body: JSON.stringify(input) });

export const updateAppointmentStatus = (id: number, status: AppointmentStatus, notes?: string) =>
  api<Appointment>(`/appointments/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status, notes: notes ?? null }),
  });

export const listAppointmentsByPet = (petId: number) =>
  api<Appointment[]>(`/appointments/pet/${petId}`);

export const getAppointmentConfig = () => api<AgendaSettings>('/appointments/config');

const pad = (n: number) => String(n).padStart(2, '0');

export function toDateTimeLocal(iso: string): string {
  const d = new Date(iso);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function fromDateTimeLocal(value: string): string {
  return new Date(value).toISOString();
}

export function defaultStartLocal(): string {
  const d = new Date();
  d.setMinutes(d.getMinutes() + 60);
  d.setSeconds(0, 0);
  d.setMinutes(Math.ceil(d.getMinutes() / 30) * 30);
  return toDateTimeLocal(d.toISOString());
}

export function toDateInput(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export function futureDateInput(days: number): string {
  return toDateInput(new Date(Date.now() + days * 86_400_000));
}

const WEEKDAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

export function parseTimeToMinutes(value: string): number {
  const [h, m] = value.split(':').map(Number);
  if (!Number.isFinite(h) || !Number.isFinite(m)) return Number.NaN;
  return h * 60 + m;
}

export function buildSlots(agenda: AgendaSettings): string[] {
  const open = parseTimeToMinutes(agenda.openingTime);
  const close = parseTimeToMinutes(agenda.closingTime);
  const step = agenda.slotMinutes;
  if (!Number.isFinite(open) || !Number.isFinite(close) || step <= 0 || close <= open) return [];
  const slots: string[] = [];
  for (let t = open; t < close; t += step) {
    slots.push(`${pad(Math.floor(t / 60))}:${pad(t % 60)}`);
  }
  return slots;
}

export function isClosedDay(agenda: AgendaSettings, date: Date): boolean {
  const name = WEEKDAY_NAMES[date.getDay()];
  return agenda.closedWeekdays.includes(name);
}

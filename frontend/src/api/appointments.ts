import { api } from './client';

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

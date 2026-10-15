import { api } from './client';

export type HospitalizationStatus =
  | 'Ingresado'
  | 'EnTratamiento'
  | 'EnRecuperacion'
  | 'Alta'
  | 'Fallecido';

export const STATUSES: HospitalizationStatus[] = [
  'Ingresado',
  'EnTratamiento',
  'EnRecuperacion',
  'Alta',
  'Fallecido',
];

export const STATUS_BADGE: Record<HospitalizationStatus, string> = {
  Ingresado: 'badge-info',
  EnTratamiento: 'badge-warning',
  EnRecuperacion: 'badge-warning',
  Alta: 'badge-success',
  Fallecido: 'badge-danger',
};

export const STATUS_LABEL: Record<HospitalizationStatus, string> = {
  Ingresado: 'Ingresado',
  EnTratamiento: 'En tratamiento',
  EnRecuperacion: 'En recuperación',
  Alta: 'Alta',
  Fallecido: 'Fallecido',
};

export const STATUS_TRANSITIONS: Record<HospitalizationStatus, HospitalizationStatus[]> = {
  Ingresado: ['EnTratamiento', 'EnRecuperacion', 'Alta', 'Fallecido'],
  EnTratamiento: ['EnRecuperacion', 'Alta', 'Fallecido'],
  EnRecuperacion: ['EnTratamiento', 'Alta', 'Fallecido'],
  Alta: [],
  Fallecido: [],
};

export const TERMINAL_STATUSES: HospitalizationStatus[] = ['Alta', 'Fallecido'];

export const isTerminal = (status: HospitalizationStatus) => TERMINAL_STATUSES.includes(status);

export interface Evolution {
  id: number;
  recordedAt: string;
  temperature: number | null;
  heartRate: number | null;
  respiratoryRate: number | null;
  observations: string;
  recordedByName: string | null;
}

export interface Medication {
  id: number;
  medicationName: string;
  dose: string;
  route: string;
  administeredAt: string;
  administeredByName: string | null;
}

export interface Hospitalization {
  id: number;
  startDate: string;
  endDate: string | null;
  status: HospitalizationStatus;
  reason: string;
  petId: number;
  petName: string | null;
  assignedVetId: number | null;
  assignedVetName: string | null;
}

export interface HospitalizationDetail extends Hospitalization {
  notes: string;
  evolutions: Evolution[];
  medications: Medication[];
}

export interface StartHospitalizationInput {
  petId: number;
  assignedVetId: number | null;
  reason: string;
  notes: string;
}

export interface EvolutionInput {
  temperature: number | null;
  heartRate: number | null;
  respiratoryRate: number | null;
  observations: string;
}

export interface MedicationInput {
  medicationName: string;
  dose: string;
  route: string;
  administeredAt: string;
}

export const listActive = () => api<Hospitalization[]>('/hospitalizations/active');

export const listHospitalizations = (filters: { status?: string; petId?: number } = {}) => {
  const params = new URLSearchParams();
  if (filters.status) params.set('status', filters.status);
  if (filters.petId) params.set('petId', String(filters.petId));
  const qs = params.toString();
  return api<Hospitalization[]>(`/hospitalizations${qs ? `?${qs}` : ''}`);
};

export const getDetail = (id: number) => api<HospitalizationDetail>(`/hospitalizations/${id}`);

export const startHospitalization = (input: StartHospitalizationInput) =>
  api<Hospitalization>('/hospitalizations', { method: 'POST', body: JSON.stringify(input) });

export const addEvolution = (id: number, input: EvolutionInput) =>
  api<HospitalizationDetail>(`/hospitalizations/${id}/evolutions`, { method: 'POST', body: JSON.stringify(input) });

export const addMedication = (id: number, input: MedicationInput) =>
  api<HospitalizationDetail>(`/hospitalizations/${id}/medications`, { method: 'POST', body: JSON.stringify(input) });

export const changeStatus = (id: number, status: HospitalizationStatus) =>
  api<HospitalizationDetail>(`/hospitalizations/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) });

export const discharge = (id: number) =>
  api<HospitalizationDetail>(`/hospitalizations/${id}/discharge`, { method: 'POST', body: '{}' });

import { api } from './client';

export type Species = 'Perro' | 'Gato' | 'Ave' | 'Reptil' | 'Exotico' | 'Otro';
export type Sex = 'Macho' | 'Hembra';

export const SPECIES: Species[] = ['Perro', 'Gato', 'Ave', 'Reptil', 'Exotico', 'Otro'];
export const SEXES: Sex[] = ['Macho', 'Hembra'];

export interface Pet {
  id: number;
  name: string;
  species: string;
  breed: string;
  birthDate: string | null;
  sex: string;
  weightKg: number;
  color: string;
  tutorId: number | null;
  ageYears: number | null;
  isSterilized: boolean;
  photoUrl: string | null;
  tutorName: string | null;
  isActive: boolean;
}

export interface TutorBasic {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  address: string;
  documentNumber: string | null;
}

export interface TutorList {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  address: string;
  documentNumber: string;
  petsCount: number;
}

export interface VaccineApplication {
  id: number;
  petId: number;
  inventoryItemId: number | null;
  vaccineName: string | null;
  applicationDate: string;
  nextDueDate: string | null;
  isDueSoon: boolean;
}

export interface UpcomingVaccine {
  petId: number;
  petName: string;
  tutorName: string | null;
  vaccineName: string | null;
  applicationDate: string;
  nextDueDate: string;
  isOverdue: boolean;
}

export interface PetInput {
  name: string;
  species: Species;
  breed: string;
  birthDate: string | null;
  sex: Sex;
  weightKg: number;
  color: string;
  tutorId: number | null;
  isSterilized: boolean;
  photoUrl: string | null;
}

export interface TutorInput {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  address: string;
  documentNumber: string | null;
}

export interface PetFilters {
  search?: string;
  species?: string;
  active?: boolean;
}

function query(params: Record<string, string | boolean | undefined>): string {
  const qs = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== '') qs.set(k, String(v));
  });
  const s = qs.toString();
  return s ? `?${s}` : '';
}

export const listPets = (filters: PetFilters = {}) =>
  api<Pet[]>(`/patients/pets${query({ search: filters.search, species: filters.species, active: filters.active })}`);

export const getPet = (id: number) => api<Pet>(`/patients/pets/${id}`);

export const createPet = (input: PetInput) =>
  api<Pet>('/patients/pets', { method: 'POST', body: JSON.stringify(input) });

export const updatePet = (id: number, input: PetInput) =>
  api<Pet>(`/patients/pets/${id}`, { method: 'PUT', body: JSON.stringify(input) });

export const setPetActive = (id: number, active: boolean) =>
  api<Pet>(`/patients/pets/${id}/active`, { method: 'PUT', body: JSON.stringify({ active }) });

export const listTutors = (signal?: AbortSignal) =>
  api<TutorBasic[]>('/patients/tutors', { signal });

export const listTutorsWithPets = () => api<TutorList[]>('/patients/tutors/list');

export const getTutor = (id: number) => api<TutorList>(`/patients/tutors/${id}`);

export const createTutor = (input: TutorInput) =>
  api<TutorBasic>('/patients/tutors', { method: 'POST', body: JSON.stringify(input) });

export const updateTutor = (id: number, input: TutorInput) =>
  api<TutorList>(`/patients/tutors/${id}`, { method: 'PUT', body: JSON.stringify(input) });

export const listVaccines = (petId: number) =>
  api<VaccineApplication[]>(`/patients/pets/${petId}/vaccines`);

export const addVaccine = (petId: number, input: { inventoryItemId: number; applicationDate: string; nextDueDate: string | null }) =>
  api<VaccineApplication>(`/patients/pets/${petId}/vaccines`, { method: 'POST', body: JSON.stringify(input) });

export const listUpcomingVaccines = (days: number) =>
  api<UpcomingVaccine[]>(`/patients/vaccines/upcoming?days=${days}`);

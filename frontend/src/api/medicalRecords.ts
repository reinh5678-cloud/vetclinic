import { api } from './client';

export interface Prescription {
  id: number;
  medicationName: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions: string;
}

export interface LabResult {
  id: number;
  testType: string;
  result: string;
  referenceRange: string;
  isAbnormal: boolean;
  resultDate: string;
}

export interface Attachment {
  id: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface MedicalRecord {
  id: number;
  recordDate: string;
  symptoms: string;
  findings: string;
  diagnosis: string;
  treatmentPlan: string;
  weightKg: number | null;
  temperatureC: number | null;
  petId: number | null;
  vetId: number | null;
  vetName: string | null;
  prescriptions: Prescription[];
  labResults: LabResult[];
  attachments: Attachment[];
}

export interface MedicalRecordInput {
  petId: number;
  vetId: number | null;
  appointmentId: number | null;
  symptoms: string;
  findings: string;
  diagnosis: string;
  treatmentPlan: string;
  weightKg: number | null;
  temperatureC: number | null;
}

export interface PrescriptionInput {
  medicationName: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions: string;
}

export interface LabResultInput {
  testType: string;
  result: string;
  referenceRange: string;
  isAbnormal: boolean;
}

export const listRecordsByPet = (petId: number) =>
  api<MedicalRecord[]>(`/medical-records/pet/${petId}`);

export const getRecord = (id: number) =>
  api<MedicalRecord>(`/medical-records/${id}`);

export const createRecord = (input: MedicalRecordInput) =>
  api<MedicalRecord>('/medical-records', { method: 'POST', body: JSON.stringify(input) });

export const updateRecord = (id: number, input: MedicalRecordInput) =>
  api<MedicalRecord>(`/medical-records/${id}`, { method: 'PUT', body: JSON.stringify(input) });

export const deleteRecord = (id: number) =>
  api<void>(`/medical-records/${id}`, { method: 'DELETE' });

export const addPrescription = (recordId: number, input: PrescriptionInput) =>
  api<MedicalRecord>(`/medical-records/${recordId}/prescriptions`, { method: 'POST', body: JSON.stringify(input) });

export const updatePrescription = (recordId: number, prescriptionId: number, input: PrescriptionInput) =>
  api<MedicalRecord>(`/medical-records/${recordId}/prescriptions/${prescriptionId}`, { method: 'PUT', body: JSON.stringify(input) });

export const deletePrescription = (recordId: number, prescriptionId: number) =>
  api<void>(`/medical-records/${recordId}/prescriptions/${prescriptionId}`, { method: 'DELETE' });

export const addLabResult = (recordId: number, input: LabResultInput) =>
  api<MedicalRecord>(`/medical-records/${recordId}/lab-results`, { method: 'POST', body: JSON.stringify(input) });

export const updateLabResult = (recordId: number, labResultId: number, input: LabResultInput) =>
  api<MedicalRecord>(`/medical-records/${recordId}/lab-results/${labResultId}`, { method: 'PUT', body: JSON.stringify(input) });

export const deleteLabResult = (recordId: number, labResultId: number) =>
  api<void>(`/medical-records/${recordId}/lab-results/${labResultId}`, { method: 'DELETE' });

export const uploadAttachment = (recordId: number, file: File) => {
  const form = new FormData();
  form.append('file', file);
  return api<MedicalRecord>(`/medical-records/${recordId}/attachments`, { method: 'POST', body: form });
};

export const deleteAttachment = (recordId: number, attachmentId: number) =>
  api<void>(`/medical-records/${recordId}/attachments/${attachmentId}`, { method: 'DELETE' });
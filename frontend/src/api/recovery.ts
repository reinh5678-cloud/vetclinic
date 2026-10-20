import { api } from './client';

export interface SecurityQuestionCatalogItem {
  questionId: number;
  text: string;
}

export interface RecoveryQuestion {
  questionId: number;
  text: string;
}

export interface RecoveryQuestions {
  username: string;
  questions: RecoveryQuestion[];
}

export interface SecurityQuestionAnswer {
  questionId: number;
  answer: string;
}

export const getQuestionCatalog = () =>
  api<SecurityQuestionCatalogItem[]>('/auth/security-questions/catalog');

export const getRecoveryQuestions = (username: string) =>
  api<RecoveryQuestions>(`/auth/recovery/questions?username=${encodeURIComponent(username)}`);

export const recoverPassword = (body: { username: string; answers: string[]; newPassword: string }) =>
  api<void>('/auth/recovery/password', { method: 'POST', body: JSON.stringify(body) });

export const getMySecurityQuestions = () =>
  api<{ questions: SecurityQuestionCatalogItem[] }>('/auth/security-questions');

export const saveSecurityQuestions = (body: { currentPassword: string; questions: SecurityQuestionAnswer[] }) =>
  api<{ questions: SecurityQuestionCatalogItem[] }>('/auth/security-questions', {
    method: 'PUT',
    body: JSON.stringify(body),
  });

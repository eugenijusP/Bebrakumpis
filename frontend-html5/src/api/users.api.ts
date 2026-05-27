import { apiFetch } from './client';
import type { UserRecord } from '../types';

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  username: string;
  password: string;
  role: string;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
}

export interface ChangePasswordRequest {
  currentPassword?: string;
  newPassword: string;
}

export async function getUsers(): Promise<UserRecord[]> {
  return apiFetch<UserRecord[]>('/api/v1/users');
}

export async function createUser(request: CreateUserRequest): Promise<UserRecord> {
  return apiFetch<UserRecord>('/api/v1/users', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateUser(id: string, request: UpdateUserRequest): Promise<UserRecord> {
  return apiFetch<UserRecord>(`/api/v1/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function changePassword(id: string, request: ChangePasswordRequest): Promise<void> {
  await apiFetch(`/api/v1/users/${id}/password`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

import { apiFetch } from './client';
import type { House } from '../types';

export interface CreateHouseRequest {
  name: string;
  bookingColor: string;
}

export interface UpdateHouseRequest {
  name: string;
  bookingColor: string;
}

export async function getHouses(): Promise<House[]> {
  return apiFetch<House[]>('/api/v1/houses');
}

export async function createHouse(request: CreateHouseRequest): Promise<House> {
  return apiFetch<House>('/api/v1/houses', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateHouse(id: string, request: UpdateHouseRequest): Promise<House> {
  return apiFetch<House>(`/api/v1/houses/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function deleteHouse(id: string): Promise<void> {
  await apiFetch(`/api/v1/houses/${id}`, { method: 'DELETE' });
}

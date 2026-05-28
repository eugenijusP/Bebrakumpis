import { apiFetch } from './client';
import type { Booking } from '../types';

export interface CreateBookingRequest {
  houseId: string;
  type: 'B' | 'R';
  startDate: string;
  endDate: string;
  displayText: string;
  notes?: string;
}

export interface UpdateBookingRequest {
  houseId: string;
  type: 'B' | 'R';
  startDate: string;
  endDate: string;
  displayText: string;
  notes?: string;
}

export async function getBookings(year: number, month: number): Promise<Booking[]> {
  return apiFetch<Booking[]>(`/api/v1/bookings?year=${year}&month=${month}`);
}

export async function createBooking(request: CreateBookingRequest): Promise<Booking> {
  return apiFetch<Booking>('/api/v1/bookings', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateBooking(id: string, request: UpdateBookingRequest): Promise<Booking> {
  return apiFetch<Booking>(`/api/v1/bookings/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function deleteBooking(id: string): Promise<void> {
  await apiFetch(`/api/v1/bookings/${id}`, { method: 'DELETE' });
}

import { renderLayout } from '../components/layout';
import { spinner, errorMessage, emptyState } from '../components/badge';
import { openModal, closeModal, closeModalOnBackdrop } from '../components/modal';
import { getHouses, createHouse, updateHouse, deleteHouse } from '../api/houses.api';
import { escHtml, escAttr } from '../utils/escHtml';
import { ApiError } from '../api/client';
import type { House } from '../types';

let _houses: House[] = [];
let _editingId: string | null = null;

export async function renderAdminHouses(): Promise<void> {
  renderLayout(spinner());

  try {
    _houses = await getHouses();
  } catch {
    document.getElementById('page-content')!.innerHTML = errorMessage('Failed to load houses.');
    return;
  }

  renderPage();
  attachEvents();
}

function renderPage(): void {
  const content = document.getElementById('page-content')!;
  content.innerHTML = `
    <div class="bh-page-header">
      <h2 class="bh-page-title">Houses</h2>
      <button id="btn-add-house" class="bh-btn bh-btn-primary">Add House</button>
    </div>
    ${_houses.length === 0
      ? emptyState('No houses found. Add one to get started.')
      : `<div class="bh-table-scroll">
          <table class="bh-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Colour</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              ${_houses.map(h => houseRow(h)).join('')}
            </tbody>
          </table>
        </div>`
    }

    <div id="modal-house" class="bh-modal" onclick="window._closeHouseModalOnBackdrop(event)">
      <div class="bh-modal-inner">
        <h3 class="bh-modal-title" id="modal-house-title">Add House</h3>
        <form id="house-form" class="bh-form" novalidate>
          <div class="bh-form-group">
            <label for="house-name" class="bh-label">Name</label>
            <input id="house-name" type="text" class="bh-input" maxlength="100" required />
          </div>
          <div class="bh-form-group">
            <label for="house-booking-color" class="bh-label">Colour</label>
            <input id="house-booking-color" type="color" class="bh-input" value="#3b82f6" />
          </div>
          <div id="house-form-error" class="bh-error-message" style="display:none"></div>
          <div style="display:flex;gap:0.75rem;justify-content:flex-end;margin-top:0.5rem">
            <button type="button" id="btn-cancel-house" class="bh-btn">Cancel</button>
            <button type="submit" id="btn-save-house" class="bh-btn bh-btn-primary">Save</button>
          </div>
        </form>
      </div>
    </div>
  `;
}

function houseRow(h: House): string {
  return `<tr>
    <td>${escHtml(h.name)}</td>
    <td>
      <span style="display:inline-flex;align-items:center;gap:0.5rem">
        <span style="width:1rem;height:1rem;border-radius:3px;background:${escAttr(h.bookingColor)};border:1px solid #d1d5db;display:inline-block"></span>
        ${escHtml(h.bookingColor)}
      </span>
    </td>
    <td style="text-align:right;white-space:nowrap">
      <button class="bh-btn bh-btn-sm" data-edit="${escAttr(h.id)}">Edit</button>
      <button class="bh-btn bh-btn-sm bh-btn-danger" data-delete="${escAttr(h.id)}" style="margin-left:0.5rem">Delete</button>
    </td>
  </tr>`;
}

function attachEvents(): void {
  (window as unknown as Record<string, unknown>)._closeHouseModalOnBackdrop =
    (e: MouseEvent) => closeModalOnBackdrop('modal-house', e);

  document.getElementById('btn-add-house')?.addEventListener('click', () => openAddModal());
  document.getElementById('btn-cancel-house')?.addEventListener('click', () => closeModal('modal-house'));
  document.getElementById('house-form')?.addEventListener('submit', handleFormSubmit);

  document.querySelectorAll('[data-edit]').forEach(btn => {
    btn.addEventListener('click', () => {
      const id = (btn as HTMLElement).dataset['edit']!;
      const house = _houses.find(h => h.id === id);
      if (house) openEditModal(house);
    });
  });

  document.querySelectorAll('[data-delete]').forEach(btn => {
    btn.addEventListener('click', () => {
      const id = (btn as HTMLElement).dataset['delete']!;
      const house = _houses.find(h => h.id === id);
      if (house) handleDelete(house);
    });
  });
}

function openAddModal(): void {
  _editingId = null;
  (document.getElementById('modal-house-title') as HTMLElement).textContent = 'Add House';
  (document.getElementById('house-name') as HTMLInputElement).value = '';
  (document.getElementById('house-booking-color') as HTMLInputElement).value = '#3b82f6';
  (document.getElementById('house-form-error') as HTMLElement).style.display = 'none';
  openModal('modal-house');
}

function openEditModal(house: House): void {
  _editingId = house.id;
  (document.getElementById('modal-house-title') as HTMLElement).textContent = 'Edit House';
  (document.getElementById('house-name') as HTMLInputElement).value = house.name;
  (document.getElementById('house-booking-color') as HTMLInputElement).value = house.bookingColor;
  (document.getElementById('house-form-error') as HTMLElement).style.display = 'none';
  openModal('modal-house');
}

async function handleFormSubmit(e: Event): Promise<void> {
  e.preventDefault();
  const errorEl = document.getElementById('house-form-error') as HTMLElement;
  const saveBtn = document.getElementById('btn-save-house') as HTMLButtonElement;
  errorEl.style.display = 'none';
  saveBtn.disabled = true;

  const name = (document.getElementById('house-name') as HTMLInputElement).value.trim();
  const bookingColor = (document.getElementById('house-booking-color') as HTMLInputElement).value;

  try {
    if (_editingId) {
      const updated = await updateHouse(_editingId, { name, bookingColor });
      _houses = _houses.map(h => h.id === _editingId ? updated : h);
    } else {
      const created = await createHouse({ name, bookingColor });
      _houses = [..._houses, created];
    }
    closeModal('modal-house');
    renderPage();
    attachEvents();
  } catch (err) {
    const msg = err instanceof ApiError ? parseApiError(err) : 'An error occurred. Please try again.';
    errorEl.textContent = msg;
    errorEl.style.display = 'block';
    saveBtn.disabled = false;
  }
}

async function handleDelete(house: House): Promise<void> {
  if (!confirm(`Delete "${house.name}"? This cannot be undone.`)) return;

  try {
    await deleteHouse(house.id);
    _houses = _houses.filter(h => h.id !== house.id);
    renderPage();
    attachEvents();
  } catch (err) {
    const msg = err instanceof ApiError && err.status === 409
      ? 'Cannot delete: this house has existing bookings.'
      : 'Failed to delete house.';
    alert(msg);
  }
}

function parseApiError(err: ApiError): string {
  try {
    const body = JSON.parse(err.message);
    return body.detail ?? 'Validation failed.';
  } catch {
    return err.message;
  }
}

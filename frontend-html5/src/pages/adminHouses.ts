import { renderLayout } from '../components/layout';
import { spinner, errorMessage, emptyState } from '../components/badge';
import { openModal, closeModal, closeModalOnBackdrop } from '../components/modal';
import { getHouses, createHouse, updateHouse, deleteHouse } from '../api/houses.api';
import { getPictures } from '../api/gallery.api';
import { escHtml, escAttr } from '../utils/escHtml';
import { ApiError } from '../api/client';
import type { House, Picture } from '../types';

let _houses: House[] = [];
let _pictures: Picture[] = [];
let _editingId: string | null = null;

export async function renderAdminHouses(): Promise<void> {
  renderLayout(spinner());

  try {
    [_houses, _pictures] = await Promise.all([getHouses(), getPictures().catch(() => [])]);
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
                <th>Description</th>
                <th>Amenities</th>
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
      <div class="bh-modal-inner" style="max-width:560px">
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
          <div class="bh-form-group">
            <label for="house-description" class="bh-label">Description</label>
            <textarea id="house-description" class="bh-input" rows="3" maxlength="2000" style="resize:vertical"></textarea>
          </div>
          <div class="bh-form-group">
            <label for="house-amenities" class="bh-label">Amenities <span style="font-weight:400;color:var(--ink-faint)">(comma-separated)</span></label>
            <input id="house-amenities" type="text" class="bh-input" placeholder="3 bedrooms, Lake view, Wood stove" />
          </div>
          <div class="bh-form-group">
            <label class="bh-label">Photo</label>
            <div id="house-photo-preview" style="margin-bottom:8px"></div>
            <input id="house-photo-url" type="text" class="bh-input" placeholder="Select from gallery or paste URL" style="margin-bottom:8px" />
            ${_pictures.length > 0 ? `
            <div style="font-size:0.8rem;color:var(--ink-faint);margin-bottom:6px">Click a photo to select it:</div>
            <div id="house-photo-picker" style="display:flex;flex-wrap:wrap;gap:6px;max-height:180px;overflow-y:auto">
              ${_pictures.map(p => `
                <img src="${escAttr(p.blobUrl)}" data-url="${escAttr(p.blobUrl)}"
                     style="width:72px;height:56px;object-fit:cover;border-radius:4px;cursor:pointer;border:2px solid transparent"
                     class="bh-photo-pick"
                     onerror="this.style.display='none'"
                     alt="gallery photo" />
              `).join('')}
            </div>` : ''}
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
  const amenitySummary = h.amenities.length
    ? escHtml(h.amenities.slice(0, 3).join(', ') + (h.amenities.length > 3 ? ` +${h.amenities.length - 3}` : ''))
    : '<span style="color:var(--ink-faint)">—</span>';
  return `<tr>
    <td>${escHtml(h.name)}</td>
    <td>
      <span style="display:inline-flex;align-items:center;gap:0.5rem">
        <span style="width:1rem;height:1rem;border-radius:3px;background:${escAttr(h.bookingColor)};border:1px solid #d1d5db;display:inline-block"></span>
        ${escHtml(h.bookingColor)}
      </span>
    </td>
    <td style="max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${h.description ? escHtml(h.description) : '<span style="color:var(--ink-faint)">—</span>'}</td>
    <td>${amenitySummary}</td>
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

  document.getElementById('house-photo-url')?.addEventListener('input', () => updatePhotoPreview());

  document.querySelectorAll<HTMLImageElement>('.bh-photo-pick').forEach(img => {
    img.addEventListener('click', () => {
      const url = img.dataset['url'] ?? '';
      (document.getElementById('house-photo-url') as HTMLInputElement).value = url;
      updatePhotoPreview();
      syncPickerHighlight(url);
    });
  });

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

function updatePhotoPreview(): void {
  const url = (document.getElementById('house-photo-url') as HTMLInputElement).value.trim();
  const preview = document.getElementById('house-photo-preview')!;
  if (url) {
    preview.innerHTML = `<img src="${escAttr(url)}" alt="preview" style="max-width:100%;max-height:120px;border-radius:4px;border:1px solid #d1d5db" onerror="this.style.display='none'" />`;
  } else {
    preview.innerHTML = '';
  }
}

function syncPickerHighlight(url: string): void {
  document.querySelectorAll<HTMLImageElement>('.bh-photo-pick').forEach(img => {
    img.style.borderColor = img.dataset['url'] === url ? 'var(--accent)' : 'transparent';
  });
}

function openAddModal(): void {
  _editingId = null;
  (document.getElementById('modal-house-title') as HTMLElement).textContent = 'Add House';
  (document.getElementById('house-name') as HTMLInputElement).value = '';
  (document.getElementById('house-booking-color') as HTMLInputElement).value = '#3b82f6';
  (document.getElementById('house-description') as HTMLTextAreaElement).value = '';
  (document.getElementById('house-amenities') as HTMLInputElement).value = '';
  (document.getElementById('house-photo-url') as HTMLInputElement).value = '';
  (document.getElementById('house-photo-preview') as HTMLElement).innerHTML = '';
  (document.getElementById('house-form-error') as HTMLElement).style.display = 'none';
  syncPickerHighlight('');
  openModal('modal-house');
}

function openEditModal(house: House): void {
  _editingId = house.id;
  (document.getElementById('modal-house-title') as HTMLElement).textContent = 'Edit House';
  (document.getElementById('house-name') as HTMLInputElement).value = house.name;
  (document.getElementById('house-booking-color') as HTMLInputElement).value = house.bookingColor;
  (document.getElementById('house-description') as HTMLTextAreaElement).value = house.description ?? '';
  (document.getElementById('house-amenities') as HTMLInputElement).value = house.amenities.join(', ');
  (document.getElementById('house-photo-url') as HTMLInputElement).value = house.photoUrl ?? '';
  (document.getElementById('house-form-error') as HTMLElement).style.display = 'none';
  updatePhotoPreview();
  syncPickerHighlight(house.photoUrl ?? '');
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
  const description = (document.getElementById('house-description') as HTMLTextAreaElement).value.trim() || null;
  const photoUrl = (document.getElementById('house-photo-url') as HTMLInputElement).value.trim() || null;
  const amenities = (document.getElementById('house-amenities') as HTMLInputElement).value
    .split(',')
    .map(a => a.trim())
    .filter(a => a.length > 0);

  try {
    if (_editingId) {
      const updated = await updateHouse(_editingId, { name, bookingColor, description, photoUrl, amenities });
      _houses = _houses.map(h => h.id === _editingId ? updated : h);
    } else {
      const created = await createHouse({ name, bookingColor, description, photoUrl, amenities });
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

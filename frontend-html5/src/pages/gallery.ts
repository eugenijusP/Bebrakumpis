import { renderLayout } from '../components/layout';
import { spinner, errorMessage } from '../components/badge';
import { getPictures, uploadPicture, updatePictureOrder, deletePicture } from '../api/gallery.api';
import { escAttr, escHtml } from '../utils/escHtml';
import { isAdmin } from '../auth';
import { ApiError } from '../api/client';
import type { Picture } from '../types';

let _pictures: Picture[] = [];

export async function renderGallery(): Promise<void> {
  renderLayout(spinner());

  try {
    _pictures = await getPictures();
  } catch {
    document.getElementById('page-content')!.innerHTML = errorMessage('Failed to load gallery.');
    return;
  }

  renderPage();
}

function renderPage(): void {
  const content = document.getElementById('page-content')!;
  const admin = isAdmin();

  content.innerHTML = `
    <div class="bh-page-header">
      <h2 class="bh-page-title">Gallery</h2>
      ${admin ? `<label class="bh-btn bh-btn-primary" style="cursor:pointer">
        Upload Photo
        <input id="upload-input" type="file" accept="image/jpeg,image/png" style="display:none" />
      </label>` : ''}
    </div>
    ${_pictures.length === 0
      ? `<div class="bh-empty-state">No photos yet.</div>`
      : `<div class="bh-gallery-grid" id="gallery-grid">
          ${_pictures.map((p, i) => pictureCard(p, i, admin)).join('')}
        </div>`}
    <div id="bh-lightbox" class="bh-lightbox">
      <button class="bh-lightbox-close" id="lightbox-close">&#215;</button>
      <img id="lightbox-img" src="" alt="" />
    </div>
    <div id="gallery-upload-error" class="bh-error-message" style="display:none;margin-top:1rem"></div>
  `;

  attachEvents(admin);
}

function pictureCard(p: Picture, index: number, admin: boolean): string {
  const adminOverlay = admin ? `
    <div class="bh-gallery-actions">
      <button class="bh-gallery-btn" data-delete="${escAttr(p.id)}" title="Delete">&#215;</button>
    </div>
    <div class="bh-gallery-reorder">
      ${index > 0 ? `<button class="bh-gallery-btn" data-move-up="${escAttr(p.id)}" title="Move up">&#8593;</button>` : ''}
      ${index < _pictures.length - 1 ? `<button class="bh-gallery-btn" data-move-down="${escAttr(p.id)}" title="Move down">&#8595;</button>` : ''}
    </div>` : '';

  return `
    <div class="bh-gallery-item" data-lightbox="${escAttr(p.blobUrl)}">
      <img src="${escAttr(p.blobUrl)}" alt="Gallery photo" loading="lazy"
           onerror="if(!this.dataset.err){this.dataset.err='1';this.src='data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22400%22 height=%22300%22%3E%3Crect width=%22400%22 height=%22300%22 fill=%22%23e5e7eb%22/%3E%3Ctext x=%2250%25%22 y=%2250%25%22 dominant-baseline=%22middle%22 text-anchor=%22middle%22 fill=%22%239ca3af%22 font-size=%2214%22 font-family=%22sans-serif%22%3EImage unavailable%3C/text%3E%3C/svg%3E';this.classList.add('bh-gallery-broken')}" />
      <div class="bh-gallery-overlay">${adminOverlay}</div>
    </div>
  `;
}

function attachEvents(admin: boolean): void {
  const grid = document.getElementById('gallery-grid');
  if (grid) {
    grid.addEventListener('click', handleGridClick);
  }

  document.getElementById('lightbox-close')?.addEventListener('click', closeLightbox);
  document.getElementById('bh-lightbox')?.addEventListener('click', (e) => {
    if ((e.target as HTMLElement).id === 'bh-lightbox') closeLightbox();
  });

  if (admin) {
    document.getElementById('upload-input')?.addEventListener('change', handleUpload);
  }
}

function handleGridClick(e: Event): void {
  const target = e.target as HTMLElement;

  const deleteId = (target.closest('[data-delete]') as HTMLElement | null)?.dataset['delete'];
  if (deleteId) { e.stopPropagation(); confirmDelete(deleteId); return; }

  const moveUpId = (target.closest('[data-move-up]') as HTMLElement | null)?.dataset['moveUp'];
  if (moveUpId) { e.stopPropagation(); movePhoto(moveUpId, 'up'); return; }

  const moveDownId = (target.closest('[data-move-down]') as HTMLElement | null)?.dataset['moveDown'];
  if (moveDownId) { e.stopPropagation(); movePhoto(moveDownId, 'down'); return; }

  const item = (target.closest('[data-lightbox]') as HTMLElement | null);
  if (item?.dataset['lightbox']) openLightbox(item.dataset['lightbox']!);
}

function openLightbox(url: string): void {
  const lb = document.getElementById('bh-lightbox')!;
  (document.getElementById('lightbox-img') as HTMLImageElement).src = url;
  lb.classList.add('bh-lightbox-open');
}

function closeLightbox(): void {
  document.getElementById('bh-lightbox')?.classList.remove('bh-lightbox-open');
}

async function handleUpload(e: Event): Promise<void> {
  const input = e.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  input.value = '';

  const errEl = document.getElementById('gallery-upload-error')!;
  errEl.style.display = 'none';

  try {
    const picture = await uploadPicture(file);
    _pictures.push(picture);
    renderPage();
  } catch (err) {
    errEl.textContent = err instanceof ApiError ? err.message : 'Upload failed.';
    errEl.style.display = 'block';
  }
}

async function confirmDelete(id: string): Promise<void> {
  if (!confirm('Delete this photo?')) return;
  try {
    await deletePicture(id);
    _pictures = _pictures.filter(p => p.id !== id);
    renderPage();
  } catch (err) {
    alert(err instanceof ApiError ? err.message : 'Failed to delete photo.');
  }
}

async function movePhoto(id: string, direction: 'up' | 'down'): Promise<void> {
  const index = _pictures.findIndex(p => p.id === id);
  if (index === -1) return;

  const swapIndex = direction === 'up' ? index - 1 : index + 1;
  if (swapIndex < 0 || swapIndex >= _pictures.length) return;

  const current = _pictures[index];
  const adjacent = _pictures[swapIndex];
  const currentOrder = current.order;
  const adjacentOrder = adjacent.order;

  try {
    await Promise.all([
      updatePictureOrder(current.id, adjacentOrder),
      updatePictureOrder(adjacent.id, currentOrder),
    ]);
    current.order = adjacentOrder;
    adjacent.order = currentOrder;
    _pictures.sort((a, b) => a.order - b.order);
    renderPage();
  } catch (err) {
    alert(err instanceof ApiError ? err.message : 'Failed to reorder photos.');
  }
}

import { renderLayout } from '../components/layout';
import { spinner, errorMessage } from '../components/badge';
import { openModal, closeModal, closeModalOnBackdrop } from '../components/modal';
import { getBookings, createBooking, updateBooking, deleteBooking } from '../api/bookings.api';
import { getHouses } from '../api/houses.api';
import { escHtml, escAttr } from '../utils/escHtml';
import { currentUser, isAdmin } from '../auth';
import { ApiError } from '../api/client';
import type { Booking, House } from '../types';

const DAYS_OF_WEEK = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

let _year = new Date().getFullYear();
let _month = new Date().getMonth() + 1;
let _bookings: Booking[] = [];
let _houses: House[] = [];
let _editingId: string | null = null;
let _tooltip: HTMLElement | null = null;

export async function renderCalendar(): Promise<void> {
  renderLayout(spinner());

  const role = currentUser()?.role ?? null;

  try {
    const [bookings, houses] = await Promise.all([
      getBookings(_year, _month),
      role !== null ? getHouses() : Promise.resolve([]),
    ]);
    _bookings = bookings.map(b => ({ ...b, startDate: b.startDate.substring(0, 10), endDate: b.endDate.substring(0, 10) }));
    _houses = houses;
  } catch {
    document.getElementById('page-content')!.innerHTML = errorMessage('Failed to load calendar.');
    return;
  }

  renderPage();
  attachEvents();
}

function renderPage(): void {
  const content = document.getElementById('page-content')!;
  const admin = isAdmin();
  const user = currentUser();

  content.innerHTML = `
    <div class="bh-page-header">
      <h2 class="bh-page-title">Calendar</h2>
      ${admin ? `<button id="btn-add-booking" class="bh-btn bh-btn-primary">Add Booking</button>` : ''}
    </div>
    <div class="bh-cal-nav">
      <button id="btn-prev-month" class="bh-btn bh-btn-sm">&#8249;</button>
      <span class="bh-cal-title">${escHtml(MONTH_NAMES[_month - 1])} ${_year}</span>
      <button id="btn-next-month" class="bh-btn bh-btn-sm">&#8250;</button>
    </div>
    <div class="bh-cal-grid" id="cal-grid">
      ${DAYS_OF_WEEK.map(d => `<div class="bh-cal-dow">${escHtml(d)}</div>`).join('')}
      ${buildDayCells(admin, user !== null)}
    </div>

    ${admin ? bookingFormModal() : ''}
  `;
}

function buildDayCells(admin: boolean, authenticated: boolean): string {
  const today = new Date();
  const firstOfMonth = new Date(_year, _month - 1, 1);
  // Monday-based: getDay() returns 0=Sun...6=Sat; convert to 0=Mon...6=Sun
  const startOffset = (firstOfMonth.getDay() + 6) % 7;
  const daysInMonth = new Date(_year, _month, 0).getDate();
  const totalCells = Math.ceil((startOffset + daysInMonth) / 7) * 7;

  const cells: string[] = [];
  for (let i = 0; i < totalCells; i++) {
    const dayNum = i - startOffset + 1;
    const isCurrentMonth = dayNum >= 1 && dayNum <= daysInMonth;
    const date = new Date(_year, _month - 1, dayNum);
    const dateStr = toDateStr(date);
    const isToday = isCurrentMonth &&
      date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate();

    const dayBookings = isCurrentMonth
      ? _bookings.filter(b => b.startDate <= dateStr && b.endDate >= dateStr)
      : [];

    cells.push(`
      <div class="bh-cal-day${!isCurrentMonth ? ' bh-cal-other-month' : ''}${isToday ? ' bh-cal-today' : ''}">
        <div class="bh-cal-day-num">${isCurrentMonth ? dayNum : ''}</div>
        ${dayBookings.map(b => bookingBand(b, dateStr, admin, authenticated)).join('')}
      </div>
    `);
  }
  return cells.join('');
}

function bookingBand(b: Booking, dateStr: string, admin: boolean, authenticated: boolean): string {
  const house = _houses.find(h => h.id === b.houseId);
  const color = house?.bookingColor ?? '#6366f1';
  const isFirst = b.startDate === dateStr || isFirstDayOfWeek(dateStr);
  const label = isFirst ? escHtml(b.displayText) : '&nbsp;';
  const reservedClass = b.type === 'R' ? ' bh-cal-band-reserved' : '';

  const editControls = admin
    ? `<span class="bh-cal-band-actions">
        <button class="bh-cal-band-btn" data-edit="${escAttr(b.id)}" title="Edit">&#9998;</button>
        <button class="bh-cal-band-btn" data-delete="${escAttr(b.id)}" title="Delete">&#215;</button>
       </span>`
    : '';

  return `<div class="bh-cal-band${reservedClass}"
    style="background-color:${escAttr(color)}"
    data-booking-id="${escAttr(b.id)}"
    data-authenticated="${authenticated}">
    ${label}${editControls}
  </div>`;
}

function isFirstDayOfWeek(dateStr: string): boolean {
  const d = new Date(dateStr);
  return d.getDay() === 1; // Monday
}

function toDateStr(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function bookingFormModal(): string {
  const houseOptions = _houses.map(h =>
    `<option value="${escAttr(h.id)}">${escHtml(h.name)}</option>`
  ).join('');

  return `
    <div id="modal-booking" class="bh-modal" onclick="window._closeBookingModalOnBackdrop(event)">
      <div class="bh-modal-inner">
        <h3 class="bh-modal-title" id="modal-booking-title">Add Booking</h3>
        <form id="booking-form" class="bh-form" novalidate>
          <div class="bh-form-group">
            <label for="booking-house" class="bh-label">House</label>
            <select id="booking-house" class="bh-input" required>
              <option value="">Select a house</option>
              ${houseOptions}
            </select>
          </div>
          <div class="bh-form-group">
            <label for="booking-type" class="bh-label">Type</label>
            <select id="booking-type" class="bh-input">
              <option value="B">Booked (confirmed)</option>
              <option value="R">Reserved (tentative)</option>
            </select>
          </div>
          <div class="bh-form-grid bh-form-grid-2">
            <div class="bh-form-group">
              <label for="booking-start" class="bh-label">Start Date</label>
              <input id="booking-start" type="date" class="bh-input" required />
            </div>
            <div class="bh-form-group">
              <label for="booking-end" class="bh-label">End Date</label>
              <input id="booking-end" type="date" class="bh-input" required />
            </div>
          </div>
          <div class="bh-form-group">
            <label for="booking-display-text" class="bh-label">Display Text <span style="color:#9ca3af">(max 50)</span></label>
            <input id="booking-display-text" type="text" class="bh-input" maxlength="50" required />
          </div>
          <div class="bh-form-group">
            <label for="booking-notes" class="bh-label">Notes</label>
            <textarea id="booking-notes" class="bh-input" rows="3" maxlength="1000"></textarea>
          </div>
          <div id="booking-form-error" class="bh-error-message" style="display:none"></div>
          <div style="display:flex;gap:0.75rem;justify-content:flex-end;margin-top:0.5rem">
            <button type="button" id="btn-cancel-booking" class="bh-btn">Cancel</button>
            <button type="submit" id="btn-save-booking" class="bh-btn bh-btn-primary">Save</button>
          </div>
        </form>
      </div>
    </div>
  `;
}

function attachEvents(): void {
  document.getElementById('btn-prev-month')?.addEventListener('click', () => changeMonth(-1));
  document.getElementById('btn-next-month')?.addEventListener('click', () => changeMonth(1));

  document.getElementById('btn-add-booking')?.addEventListener('click', () => {
    _editingId = null;
    openBookingModal();
  });

  document.getElementById('btn-cancel-booking')?.addEventListener('click', () => closeModal('modal-booking'));

  document.getElementById('booking-form')?.addEventListener('submit', async e => {
    e.preventDefault();
    await saveBooking();
  });

  (window as unknown as Record<string, unknown>)._closeBookingModalOnBackdrop =
    (e: MouseEvent) => closeModalOnBackdrop('modal-booking', e);

  const grid = document.getElementById('cal-grid');
  if (grid) {
    grid.addEventListener('click', handleGridClick);
    grid.addEventListener('mouseover', handleBandMouseover);
    grid.addEventListener('mouseout', handleBandMouseout);
  }
}

function handleGridClick(e: Event): void {
  const target = e.target as HTMLElement;

  const editId = (target.closest('[data-edit]') as HTMLElement | null)?.dataset['edit'];
  if (editId) {
    _editingId = editId;
    openBookingModal(editId);
    return;
  }

  const deleteId = (target.closest('[data-delete]') as HTMLElement | null)?.dataset['delete'];
  if (deleteId) {
    confirmDelete(deleteId);
  }
}

function handleBandMouseover(e: Event): void {
  const band = (e.target as HTMLElement).closest('.bh-cal-band') as HTMLElement | null;
  if (!band) return;
  const authenticated = band.dataset['authenticated'] === 'true';
  if (!authenticated) return;

  const bookingId = band.dataset['bookingId'];
  const booking = _bookings.find(b => b.id === bookingId);
  if (!booking) return;

  showTooltip(e as MouseEvent, booking);
}

function handleBandMouseout(e: Event): void {
  const rel = (e as MouseEvent).relatedTarget as HTMLElement | null;
  if (!rel?.closest('.bh-cal-band')) {
    hideTooltip();
  }
}

function showTooltip(e: MouseEvent, b: Booking): void {
  hideTooltip();
  _tooltip = document.createElement('div');
  _tooltip.className = 'bh-cal-tooltip';
  _tooltip.innerHTML = `
    <div class="bh-cal-tooltip-title">${escHtml(b.displayText)}</div>
    <div class="bh-cal-tooltip-row">${escHtml(b.startDate)} – ${escHtml(b.endDate)}</div>
    ${b.notes ? `<div class="bh-cal-tooltip-row">${escHtml(b.notes)}</div>` : ''}
    ${b.createdByName ? `<div class="bh-cal-tooltip-row">Added by ${escHtml(b.createdByName)}${b.createdAt ? ` on ${new Date(b.createdAt).toLocaleDateString()}` : ''}</div>` : ''}
  `;
  document.body.appendChild(_tooltip);
  positionTooltip(e);
}

function positionTooltip(e: MouseEvent): void {
  if (!_tooltip) return;
  const x = Math.min(e.clientX + 12, window.innerWidth - 280);
  const y = Math.min(e.clientY + 12, window.innerHeight - 160);
  _tooltip.style.left = `${x}px`;
  _tooltip.style.top = `${y}px`;
}

function hideTooltip(): void {
  _tooltip?.remove();
  _tooltip = null;
}

function openBookingModal(editId?: string): void {
  const booking = editId ? _bookings.find(b => b.id === editId) : null;

  const titleEl = document.getElementById('modal-booking-title');
  if (titleEl) titleEl.textContent = booking ? 'Edit Booking' : 'Add Booking';

  (document.getElementById('booking-house') as HTMLSelectElement).value = booking?.houseId ?? '';
  (document.getElementById('booking-type') as HTMLSelectElement).value = booking?.type ?? 'B';
  (document.getElementById('booking-start') as HTMLInputElement).value = booking?.startDate ?? '';
  (document.getElementById('booking-end') as HTMLInputElement).value = booking?.endDate ?? '';
  (document.getElementById('booking-display-text') as HTMLInputElement).value = booking?.displayText ?? '';
  (document.getElementById('booking-notes') as HTMLTextAreaElement).value = booking?.notes ?? '';

  const errEl = document.getElementById('booking-form-error')!;
  errEl.style.display = 'none';
  errEl.textContent = '';

  openModal('modal-booking');
}

async function saveBooking(): Promise<void> {
  const houseId = (document.getElementById('booking-house') as HTMLSelectElement).value;
  const type = (document.getElementById('booking-type') as HTMLSelectElement).value as 'B' | 'R';
  const startDate = (document.getElementById('booking-start') as HTMLInputElement).value;
  const endDate = (document.getElementById('booking-end') as HTMLInputElement).value;
  const displayText = (document.getElementById('booking-display-text') as HTMLInputElement).value.trim();
  const notes = (document.getElementById('booking-notes') as HTMLTextAreaElement).value.trim() || undefined;

  const errEl = document.getElementById('booking-form-error')!;
  errEl.style.display = 'none';

  const saveBtn = document.getElementById('btn-save-booking') as HTMLButtonElement;
  saveBtn.disabled = true;

  try {
    if (_editingId) {
      await updateBooking(_editingId, { houseId, type, startDate, endDate, displayText, notes });
    } else {
      await createBooking({ houseId, type, startDate, endDate, displayText, notes });
    }
    closeModal('modal-booking');
    await reloadBookings();
  } catch (err) {
    const msg = err instanceof ApiError ? err.message : 'Failed to save booking.';
    errEl.textContent = msg;
    errEl.style.display = 'block';
  } finally {
    saveBtn.disabled = false;
  }
}

async function confirmDelete(id: string): Promise<void> {
  const booking = _bookings.find(b => b.id === id);
  if (!confirm(`Delete booking "${booking?.displayText ?? id}"?`)) return;

  try {
    await deleteBooking(id);
    await reloadBookings();
  } catch (err) {
    alert(err instanceof ApiError ? err.message : 'Failed to delete booking.');
  }
}

async function changeMonth(delta: number): Promise<void> {
  _month += delta;
  if (_month > 12) { _month = 1; _year++; }
  if (_month < 1) { _month = 12; _year--; }
  await reloadBookings();
}

async function reloadBookings(): Promise<void> {
  try {
    const fresh = await getBookings(_year, _month);
    _bookings = fresh.map(b => ({ ...b, startDate: b.startDate.substring(0, 10), endDate: b.endDate.substring(0, 10) }));
  } catch {
    // keep previous bookings on error
  }
  renderPage();
  attachEvents();
}

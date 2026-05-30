import { renderLayout } from '../components/layout';
import { spinner, errorMessage } from '../components/badge';
import { getBookings } from '../api/bookings.api';
import { getPictures } from '../api/gallery.api';
import { escHtml, escAttr } from '../utils/escHtml';
import { currentUser } from '../auth';
import type { Booking, Picture } from '../types';

const DAYS_OF_WEEK = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

export async function renderMainPage(): Promise<void> {
  renderLayout(spinner());

  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth() + 1;

  let bookings: Booking[] = [];
  let pictures: Picture[] = [];

  try {
    [bookings, pictures] = await Promise.all([
      getBookings(year, month),
      getPictures(),
    ]);
    bookings = bookings.map(b => ({
      ...b,
      startDate: b.startDate.substring(0, 10),
      endDate: b.endDate.substring(0, 10),
    }));
  } catch {
    document.getElementById('page-content')!.innerHTML = errorMessage('Failed to load page content.');
    return;
  }

  const authenticated = currentUser() !== null;
  const teaserPhotos = pictures.slice(0, 6);

  document.getElementById('page-content')!.innerHTML = `
    <div class="bh-hero">
      <div class="bh-hero-body">
        <h1 class="bh-hero-title">Welcome to Bebrakumpis</h1>
        <p class="bh-hero-text">
          Bebrakumpis is a peaceful countryside retreat nestled in the heart of Lithuania.
          Two cosy houses — perfect for family gatherings, quiet weekends, or celebrations with friends.
          Surrounded by nature, away from the noise of the city.
        </p>
        <p class="bh-hero-text">
          Browse availability below and explore the gallery to see both houses in full.
        </p>
      </div>
    </div>

    <div class="bh-section">
      <h2 class="bh-section-title">Availability — ${escHtml(MONTH_NAMES[month - 1])} ${year}</h2>
      <div class="bh-cal-grid">
        ${DAYS_OF_WEEK.map(d => `<div class="bh-cal-dow">${escHtml(d)}</div>`).join('')}
        ${buildDayCells(year, month, bookings, authenticated)}
      </div>
    </div>

    <div class="bh-section">
      <div class="bh-section-header">
        <h2 class="bh-section-title">Gallery</h2>
        <a href="#/gallery" class="bh-gallery-teaser-link">View all photos &#8594;</a>
      </div>
      ${teaserPhotos.length === 0
        ? `<div class="bh-empty-state">No photos yet.</div>`
        : `<div class="bh-gallery-teaser-grid">
            ${teaserPhotos.map(p => teaserCard(p)).join('')}
          </div>
          <div style="margin-top:1rem;text-align:right">
            <a href="#/gallery" class="bh-gallery-teaser-link">View all photos &#8594;</a>
          </div>`}
    </div>
  `;
}

function buildDayCells(year: number, month: number, bookings: Booking[], authenticated: boolean): string {
  const today = new Date();
  const firstOfMonth = new Date(year, month - 1, 1);
  const startOffset = (firstOfMonth.getDay() + 6) % 7;
  const daysInMonth = new Date(year, month, 0).getDate();
  const totalCells = Math.ceil((startOffset + daysInMonth) / 7) * 7;

  const cells: string[] = [];
  for (let i = 0; i < totalCells; i++) {
    const dayNum = i - startOffset + 1;
    const isCurrentMonth = dayNum >= 1 && dayNum <= daysInMonth;
    const date = new Date(year, month - 1, dayNum);
    const dateStr = toDateStr(date);
    const isToday =
      isCurrentMonth &&
      date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate();

    const dayBookings = isCurrentMonth
      ? bookings.filter(b => b.startDate <= dateStr && b.endDate >= dateStr)
      : [];

    cells.push(`
      <div class="bh-cal-day${!isCurrentMonth ? ' bh-cal-other-month' : ''}${isToday ? ' bh-cal-today' : ''}">
        <div class="bh-cal-day-num">${isCurrentMonth ? dayNum : ''}</div>
        ${dayBookings.map(b => bookingBand(b, dateStr, authenticated)).join('')}
      </div>
    `);
  }
  return cells.join('');
}

function bookingBand(b: Booking, dateStr: string, authenticated: boolean): string {
  const isFirst = b.startDate === dateStr || isFirstDayOfWeek(dateStr);
  const label = isFirst ? escHtml(b.displayText) : '&nbsp;';
  const reservedClass = b.type === 'R' ? ' bh-cal-band-reserved' : '';
  const color = b.type === 'B' ? '#6366f1' : '#ef4444';

  return `<div class="bh-cal-band${reservedClass}"
    style="background-color:${escAttr(color)}"
    data-booking-id="${escAttr(b.id)}"
    data-authenticated="${authenticated}">
    ${label}
  </div>`;
}

function isFirstDayOfWeek(dateStr: string): boolean {
  return new Date(dateStr).getDay() === 1;
}

function toDateStr(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function teaserCard(p: Picture): string {
  return `
    <div class="bh-gallery-teaser-item">
      <img src="${escAttr(p.blobUrl)}" alt="Gallery photo" loading="lazy"
           onerror="if(!this.dataset.err){this.dataset.err='1';this.src='data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22400%22 height=%22300%22%3E%3Crect width=%22400%22 height=%22300%22 fill=%22%23e5e7eb%22/%3E%3Ctext x=%2250%25%22 y=%2250%25%22 dominant-baseline=%22middle%22 text-anchor=%22middle%22 fill=%22%239ca3af%22 font-size=%2214%22 font-family=%22sans-serif%22%3EImage unavailable%3C/text%3E%3C/svg%3E';this.classList.add('bh-gallery-broken')}" />
    </div>
  `;
}

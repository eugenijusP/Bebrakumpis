import { renderLayout } from '../components/layout';
import { spinner, errorMessage } from '../components/badge';
import { getBookings } from '../api/bookings.api';
import { getPictures } from '../api/gallery.api';
import { getHouses } from '../api/houses.api';
import { escHtml, escAttr } from '../utils/escHtml';
import type { Booking, Picture } from '../types';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

const MAP_EMBED = 'https://maps.google.com/maps?q=55.2034698,25.4592577&z=14&hl=en&output=embed';
const MAP_LINK = 'https://www.google.com/maps/place/Bebrakumpis/@55.2034728,25.4566774,17z';

// Marketing copy keyed by house name (names + booking colours come from the API;
// blurb/tagline/features are presentation-only and fall back to a generic cottage).
interface HouseCopy { tagline: string; blurb: string; feats: string[]; }

const HOUSE_COPY: Record<string, HouseCopy> = {
  'Pirkia': {
    tagline: 'The old farmhouse',
    blurb: 'The original log farmhouse with a wood stove, a long oak table that seats the whole family, and a covered porch over the water. Warm in winter, cool under the pines in summer.',
    feats: ['3 bedrooms', 'Wood-fired stove', 'Big kitchen', 'Lakeside porch'],
  },
  'Kl\u0117tis': {
    tagline: 'The timber granary',
    blurb: 'A restored granary turned cosy guest cabin, closest to the shore. Two snug bedrooms, a little kitchenette, and the best morning light over Lake Bebrusai.',
    feats: ['2 bedrooms', 'Kitchenette', 'Reading nook', 'Lake view'],
  },
};

const DEFAULT_COPY: HouseCopy = {
  tagline: 'Lakeside cottage',
  blurb: 'A cosy timber house on the wooded shore of Lake Bebrusai \u2014 perfect for a quiet weekend by the water.',
  feats: ['Lake view', 'Forest at the door'],
};

// Used only if the houses endpoint is unavailable to public visitors.
const FALLBACK_HOUSES = [
  { name: 'Pirkia', bookingColor: '#bd5f3a' },
  { name: 'Kl\u0117tis', bookingColor: '#4f6850' },
];

export async function renderMainPage(): Promise<void> {
  renderLayout(spinner());

  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth() + 1;

  let bookings: Booking[] = [];
  let pictures: Picture[] = [];
  let houses: { name: string; bookingColor: string }[] = [];

  try {
    [bookings, pictures, houses] = await Promise.all([
      getBookings(year, month),
      getPictures(),
      getHouses().catch(() => []),
    ]);
    bookings = bookings.map((b) => ({
      ...b,
      startDate: b.startDate.substring(0, 10),
      endDate: b.endDate.substring(0, 10),
    }));
  } catch {
    document.getElementById('page-content')!.innerHTML = errorMessage('Failed to load page content.');
    return;
  }

  const houseList = houses.length ? houses : FALLBACK_HOUSES;
  const teaser = pictures.slice(0, 6);

  document.getElementById('page-content')!.innerHTML = `
    <section class="bh-hero">
      <div class="bh-hero-copy">
        <div class="eyebrow">On Lake Bebrusai &middot; Mol&#279;tai, Lithuania</div>
        <h1>A quiet corner of the countryside called <em>Bebrakumpis</em></h1>
        <p>Two timber houses on the wooded shore of <strong>Lake Bebrusai</strong>, kept by our family and open to friends and a few guests each summer. Swim from the doorstep, wood smoke, and long tables.</p>
        <div class="bh-hero-actions">
          <a href="#/calendar" class="bh-btn bh-btn-primary">Check availability</a>
          <a href="#/gallery" class="bh-btn bh-btn-ghost">See the gallery</a>
        </div>
        <div class="bh-hero-meta">
          <div><div class="n">${houseList.length}</div><div class="l">${houseList.length === 1 ? 'cottage' : 'cottages'}</div></div>
          <div><div class="n">5</div><div class="l">bedrooms</div></div>
          <div><div class="n">200m</div><div class="l">of shoreline</div></div>
        </div>
      </div>
      <div class="bh-hero-art">
        <img src="/hero.webp" alt="Bebrakumpis on Lake Bebrusai" />
        <div class="bh-hero-badge">
          <div class="pin">&#9678;</div>
          <div><div class="t">On Lake Bebrusai</div><div class="s">Swim from the doorstep</div></div>
        </div>
      </div>
    </section>

    <section class="bh-section">
      <div class="bh-section-head">
        <div class="eyebrow">Two houses, one yard</div>
        <h2>Room for the whole family &mdash; or just the two of you</h2>
        <p>Book them together for a big gathering, or take just one for a quiet weekend by the water.</p>
      </div>
      <div class="bh-houses-grid">
        ${houseList.map(houseCard).join('')}
      </div>
    </section>

    <section class="bh-section">
      <div class="bh-avail-band">
        <div>
          <div class="eyebrow">Plan your stay</div>
          <h2>See when each house is free</h2>
          <p>One shared calendar for both cottages. Booked dates are confirmed; reserved dates are tentative holds. Have a look, then send us a message to claim your weekend.</p>
          <div style="margin-top:22px"><a href="#/calendar" class="bh-btn bh-btn-primary">Open the calendar</a></div>
        </div>
        ${miniCal(year, month, bookings)}
      </div>
    </section>

    <section class="bh-section">
      <div class="bh-section-head">
        <div class="eyebrow">Finding us</div>
        <h2>On the shore of Lake Bebrusai</h2>
      </div>
      <div class="bh-loc-grid">
        <div class="bh-loc-info">
          <p>Bebrakumpis sits right on the water in the Mol&#279;tai lake district of eastern Lithuania &mdash; forest behind, lake in front &mdash; about an hour and a half from Vilnius by car. The last stretch is a gravel lane, so drive slowly; the beavers have right of way.</p>
          <div class="bh-loc-list">
            <div class="bh-loc-item"><div class="ic">&#9678;</div><div><div class="t">Lakefront on Lake Bebrusai</div><div class="s">Private shoreline, jetty &amp; swimming</div></div></div>
            <div class="bh-loc-item"><div class="ic">&#9650;</div><div><div class="t">Pine forest at the door</div><div class="s">Mushrooms, berries, long walks</div></div></div>
            <div class="bh-loc-item"><div class="ic">&#9654;</div><div><div class="t">~90 minutes from Vilnius</div><div class="s">Parking for several cars in the yard</div></div></div>
          </div>
          <a class="bh-btn bh-btn-ghost bh-btn-sm" href="${MAP_LINK}" target="_blank" rel="noopener">Open in Google Maps</a>
        </div>
        <div class="bh-loc-map">
          <iframe src="${MAP_EMBED}" loading="lazy" referrerpolicy="no-referrer-when-downgrade" title="Map of Bebrakumpis"></iframe>
        </div>
      </div>
    </section>

    <section class="bh-section">
      <div class="bh-section-header">
        <div class="bh-section-head" style="margin-bottom:0">
          <div class="eyebrow">A look around</div>
          <h2>From the gallery</h2>
        </div>
        <a href="#/gallery" class="bh-teaser-link">View all photos &#8594;</a>
      </div>
      ${teaser.length === 0
        ? `<div class="bh-empty-state">No photos yet.</div>`
        : `<div class="bh-teaser-grid">${teaser.map(teaserCard).join('')}</div>`}
    </section>
  `;
}

function houseCard(h: { name: string; bookingColor: string }): string {
  const copy = HOUSE_COPY[h.name] ?? DEFAULT_COPY;
  return `
    <article class="bh-house-card">
      <div class="bh-house-img"><div class="ph">${escHtml(h.name)} &mdash; photo</div></div>
      <div class="bh-house-body">
        <div class="row">
          <div>
            <div class="eyebrow" style="color:var(--ink-faint)">${escHtml(copy.tagline)}</div>
            <h3 style="margin-top:4px">${escHtml(h.name)}</h3>
          </div>
          <span class="bh-house-swatch" style="background:${escAttr(h.bookingColor)};margin-top:8px"></span>
        </div>
        <p>${escHtml(copy.blurb)}</p>
        <div class="bh-house-feats">${copy.feats.map((f) => `<span class="bh-tag">${escHtml(f)}</span>`).join('')}</div>
        <div style="margin-top:20px"><a href="#/calendar" class="bh-btn bh-btn-ghost bh-btn-sm">Check ${escHtml(h.name)} dates</a></div>
      </div>
    </article>
  `;
}

function teaserCard(p: Picture): string {
  return `
    <a href="#/gallery" class="bh-teaser-item">
      <img src="${escAttr(p.blobUrl)}" alt="Gallery photo" loading="lazy"
           onerror="if(!this.dataset.err){this.dataset.err='1';this.src='data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22400%22 height=%22300%22%3E%3Crect width=%22400%22 height=%22300%22 fill=%22%23f1e7d6%22/%3E%3Ctext x=%2250%25%22 y=%2250%25%22 dominant-baseline=%22middle%22 text-anchor=%22middle%22 fill=%22%239c9084%22 font-size=%2214%22 font-family=%22monospace%22%3EImage unavailable%3C/text%3E%3C/svg%3E';this.classList.add('bh-gallery-broken')}" />
    </a>
  `;
}

function miniCal(year: number, month: number, bookings: Booking[]): string {
  const status: Record<string, 'b' | 'r'> = {};
  bookings.forEach((b) => {
    const d = new Date(b.startDate + 'T00:00');
    const end = new Date(b.endDate + 'T00:00');
    while (d <= end) {
      const s = toDateStr(d);
      if (status[s] !== 'b') status[s] = b.type === 'B' ? 'b' : 'r';
      d.setDate(d.getDate() + 1);
    }
  });

  const firstOfMonth = new Date(year, month - 1, 1);
  const startOffset = (firstOfMonth.getDay() + 6) % 7;
  const daysInMonth = new Date(year, month, 0).getDate();
  const total = Math.ceil((startOffset + daysInMonth) / 7) * 7;

  let cells = '';
  for (let i = 0; i < total; i++) {
    const dayNum = i - startOffset + 1;
    const inMonth = dayNum >= 1 && dayNum <= daysInMonth;
    const st = inMonth ? status[toDateStr(new Date(year, month - 1, dayNum))] : undefined;
    const cls = ['bh-mini-cell', inMonth ? 'on' : '', st ?? ''].filter(Boolean).join(' ');
    cells += `<div class="${cls}">${inMonth ? dayNum : ''}</div>`;
  }

  return `
    <div class="bh-mini-cal">
      <div class="bh-mini-head"><span>${escHtml(MONTH_NAMES[month - 1])} ${year}</span><span>M T W T F S S</span></div>
      <div class="bh-mini-grid">${cells}</div>
    </div>
  `;
}

function toDateStr(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

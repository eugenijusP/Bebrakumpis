import { currentUser, isAdmin, logout, initAuth } from '../auth';
import { login } from '../api/auth.api';
import { ApiError } from '../api/client';
import { router } from '../router';
import { escHtml } from '../utils/escHtml';

export function renderLayout(content: string): void {
  const user = currentUser();
  const adminLinks = isAdmin()
    ? `<a href="#/admin/houses" class="bh-nav-link">Manage Houses</a>
       <a href="#/admin/users" class="bh-nav-link">Users</a>`
    : '';

  const authArea = user
    ? `<div class="bh-nav-user">
         <span class="bh-user-name">${escHtml(user.username)}</span>
         <button id="btn-logout" class="bh-btn bh-btn-sm bh-btn-ghost">Log out</button>
       </div>`
    : `<button id="btn-open-login" class="bh-nav-cta">Log in</button>`;

  document.getElementById('app')!.innerHTML = `
    <header class="bh-nav-bar">
      <div class="bh-wrap bh-nav-inner">
        <a href="#/" class="bh-brand"><span class="dot"></span> Bebrakumpis <small>est. &rsquo;98</small></a>
        <nav class="bh-nav-links">
          <a href="#/" class="bh-nav-link">Home</a>
          <a href="#/gallery" class="bh-nav-link">Gallery</a>
          <a href="#/calendar" class="bh-nav-link">Calendar</a>
          ${adminLinks}
          ${authArea}
        </nav>
      </div>
    </header>
    <main class="bh-main">
      <div id="page-content">${content}</div>
    </main>
    <footer class="bh-footer">
      <div class="bh-footer-inner">
        <div>
          <a href="#/" class="bh-brand" style="margin-bottom:8px"><span class="dot"></span> Bebrakumpis</a>
          <div><small>A family retreat on the shore of Lake Bebrusai &middot; Mol&#279;tai, Lithuania</small></div>
        </div>
        <div class="bh-footer-links">
          <a href="#/">Home</a>
          <a href="#/gallery">Gallery</a>
          <a href="#/calendar">Calendar</a>
          <a href="mailto:hello@bebrakumpis.lt">hello@bebrakumpis.lt</a>
        </div>
      </div>
    </footer>

    <div id="modal-login" class="bh-modal" onclick="if(event.target.id==='modal-login')window._closeLoginModal()">
      <div class="bh-modal-inner" style="max-width:420px">
        <div class="eyebrow">Family &amp; caretakers</div>
        <h3 class="bh-modal-title" style="margin:8px 0 6px">Welcome back</h3>
        <p class="muted" style="margin-bottom:1.4rem;font-size:0.95rem">Sign in to manage houses, photos &amp; bookings.</p>
        <form id="login-form" class="bh-form" novalidate>
          <div class="bh-form-group">
            <label for="login-username" class="bh-label">Username</label>
            <input id="login-username" type="text" class="bh-input" autocomplete="username" required />
          </div>
          <div class="bh-form-group">
            <label for="login-password" class="bh-label">Password</label>
            <input id="login-password" type="password" class="bh-input" autocomplete="current-password" required />
          </div>
          <div id="login-error" class="bh-error-message" style="display:none"></div>
          <div style="display:flex;gap:0.75rem;justify-content:flex-end;margin-top:0.25rem">
            <button type="button" id="btn-login-cancel" class="bh-btn bh-btn-ghost">Cancel</button>
            <button type="submit" id="login-btn" class="bh-btn bh-btn-primary">Log in</button>
          </div>
        </form>
        <div class="bh-login-demo">demo &middot; admin / Admin@123</div>
      </div>
    </div>
  `;

  highlightActiveNav();
  wireLogin();

  document.getElementById('btn-logout')?.addEventListener('click', async () => {
    await logout();
    router.navigate('#/');
  });
}

function highlightActiveNav(): void {
  const hash = window.location.hash || '#/';
  document.querySelectorAll<HTMLAnchorElement>('.bh-nav-link').forEach((a) => {
    const href = a.getAttribute('href') ?? '';
    const isHome = href === '#/' && (hash === '#/' || hash === '' || hash === '#/login');
    if (isHome || (href !== '#/' && hash.startsWith(href))) {
      a.classList.add('active');
    }
  });
}

export function openLoginModal(): void {
  document.getElementById('modal-login')?.classList.add('bh-modal-open');
  (document.getElementById('login-username') as HTMLInputElement | null)?.focus();
}

function closeLoginModal(): void {
  document.getElementById('modal-login')?.classList.remove('bh-modal-open');
}

function wireLogin(): void {
  (window as unknown as Record<string, unknown>)._closeLoginModal = closeLoginModal;

  document.getElementById('btn-open-login')?.addEventListener('click', openLoginModal);
  document.getElementById('btn-login-cancel')?.addEventListener('click', closeLoginModal);

  const form = document.getElementById('login-form') as HTMLFormElement | null;
  form?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const errEl = document.getElementById('login-error') as HTMLDivElement;
    const btn = document.getElementById('login-btn') as HTMLButtonElement;
    errEl.style.display = 'none';
    btn.disabled = true;
    btn.textContent = 'Logging in\u2026';

    const username = (document.getElementById('login-username') as HTMLInputElement).value.trim();
    const password = (document.getElementById('login-password') as HTMLInputElement).value;

    try {
      await login(username, password);
      await initAuth();
      closeLoginModal();
      // Re-render: if we were on the /login redirect, go home; otherwise refresh in place.
      const hash = window.location.hash;
      if (hash === '#/login' || hash === '') {
        if (hash === '') router.navigate('#/'); else window.location.hash = '#/';
      } else {
        router.navigate(hash);
      }
    } catch (err) {
      errEl.textContent = err instanceof ApiError && err.status === 401
        ? 'Invalid username or password.'
        : 'Login failed. Please try again.';
      errEl.style.display = 'block';
      btn.disabled = false;
      btn.textContent = 'Log in';
    }
  });
}

import { login } from '../api/auth.api';
import { initAuth } from '../auth';
import { ApiError } from '../api/client';

export async function renderLogin(): Promise<void> {
  document.getElementById('app')!.innerHTML = `
    <div class="bh-login-page">
      <div class="bh-login-card">
        <h1 class="bh-login-title">Welcome back</h1>
        <p class="bh-login-sub">Sign in to manage houses, photos &amp; bookings.</p>
        <form id="login-form" class="bh-form" novalidate>
          <div class="bh-form-group">
            <label for="username" class="bh-label">Username</label>
            <input
              id="username"
              name="username"
              type="text"
              class="bh-input"
              autocomplete="username"
              required
            />
          </div>
          <div class="bh-form-group">
            <label for="password" class="bh-label">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              class="bh-input"
              autocomplete="current-password"
              required
            />
          </div>
          <div id="login-error" class="bh-error-message" style="display:none"></div>
          <button type="submit" id="login-btn" class="bh-btn bh-btn-primary bh-btn-full">
            Log in
          </button>
        </form>
        <div class="bh-login-demo">demo &middot; admin / Admin@123</div>
      </div>
    </div>
  `;

  const form = document.getElementById('login-form') as HTMLFormElement;
  const errorEl = document.getElementById('login-error') as HTMLDivElement;
  const btn = document.getElementById('login-btn') as HTMLButtonElement;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    errorEl.style.display = 'none';
    btn.disabled = true;
    btn.textContent = 'Logging in…';

    const username = (document.getElementById('username') as HTMLInputElement).value.trim();
    const password = (document.getElementById('password') as HTMLInputElement).value;

    try {
      await login(username, password);
      await initAuth();
      window.location.hash = '#/';
    } catch (err) {
      const message =
        err instanceof ApiError && err.status === 401
          ? 'Invalid username or password.'
          : 'Login failed. Please try again.';
      errorEl.textContent = message;
      errorEl.style.display = 'block';
      btn.disabled = false;
      btn.textContent = 'Log in';
    }
  });
}

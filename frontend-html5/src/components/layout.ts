import { currentUser, isAdmin, logout } from '../auth';
import { escHtml } from '../utils/escHtml';

export function renderLayout(content: string): void {
  const user = currentUser();
  const adminLinks = isAdmin()
    ? `<a href="#/admin/houses" class="bh-nav-link">Manage Houses</a>
        <a href="#/admin/users" class="bh-nav-link">User Management</a>`
    : '';

  document.getElementById('app')!.innerHTML = `
    <aside class="bh-sidebar">
      <div class="bh-sidebar-brand">Bebrakumpis</div>
      <nav class="bh-nav">
        <a href="#/" class="bh-nav-link">Home</a>
        <a href="#/houses" class="bh-nav-link">Houses</a>
        <a href="#/calendar" class="bh-nav-link">Calendar</a>
        <a href="#/gallery" class="bh-nav-link">Gallery</a>
        ${adminLinks}
      </nav>
      <div class="bh-sidebar-footer">
        ${user
          ? `<span class="bh-user-name">${escHtml(user.username)}</span>
             <button id="btn-logout" class="bh-btn bh-btn-sm">Log out</button>`
          : `<a href="#/login" class="bh-nav-link">Log in</a>`}
      </div>
    </aside>
    <main class="bh-main">
      <div id="page-content">${content}</div>
    </main>
  `;

  document.getElementById('btn-logout')?.addEventListener('click', async () => {
    await logout();
    window.location.hash = '#/login';
  });
}

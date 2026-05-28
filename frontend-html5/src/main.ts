import { initAuth, currentUser, isAdmin } from './auth';
import { renderLayout } from './components/layout';
import { spinner } from './components/badge';
import { renderLogin } from './pages/login';
import { renderAdminHouses } from './pages/adminHouses';
import { renderAdminUsers } from './pages/adminUsers';
import { renderCalendar } from './pages/calendar';
import { router } from './router';

function requireAuth(): boolean {
  if (!currentUser()) {
    window.location.hash = '#/login';
    return false;
  }
  return true;
}

router.on('/login', async () => {
  await renderLogin();
});

router.on('/', async () => {
  if (!requireAuth()) return;
  renderLayout('<h2>Welcome to Bebrakumpis</h2><p>Use the sidebar to navigate.</p>');
});

router.on('/calendar', async () => {
  await renderCalendar();
});

router.on('/admin/houses', async () => {
  if (!requireAuth()) return;
  if (!isAdmin()) {
    window.location.hash = '#/';
    return;
  }
  await renderAdminHouses();
});

router.on('/admin/users', async () => {
  if (!requireAuth()) return;
  if (!isAdmin()) {
    window.location.hash = '#/';
    return;
  }
  await renderAdminUsers();
});

(async () => {
  await initAuth();
  router.start();
})();

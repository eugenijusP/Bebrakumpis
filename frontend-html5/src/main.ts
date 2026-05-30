import { initAuth, currentUser, isAdmin } from './auth';
import { openLoginModal } from './components/layout';
import { renderAdminHouses } from './pages/adminHouses';
import { renderAdminUsers } from './pages/adminUsers';
import { renderCalendar } from './pages/calendar';
import { renderGallery } from './pages/gallery';
import { renderMainPage } from './pages/main';
import { router } from './router';

function requireAuth(): boolean {
  if (!currentUser()) {
    window.location.hash = '#/login';
    return false;
  }
  return true;
}

router.on('/login', async () => {
  // Login is a modal, not a page: render home behind it, then pop the modal.
  await renderMainPage();
  openLoginModal();
});

router.on('/', async () => {
  await renderMainPage();
});

router.on('/calendar', async () => {
  await renderCalendar();
});

router.on('/gallery', async () => {
  await renderGallery();
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

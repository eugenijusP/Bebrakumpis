import { initAuth, currentUser } from './auth';
import { renderLayout } from './components/layout';
import { spinner } from './components/badge';
import { renderLogin } from './pages/login';
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

(async () => {
  await initAuth();
  router.start();
})();

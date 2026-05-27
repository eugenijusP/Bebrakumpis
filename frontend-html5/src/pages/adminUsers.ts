import { renderLayout } from '../components/layout';
import { spinner, errorMessage, emptyState, roleBadge } from '../components/badge';
import { openModal, closeModal, closeModalOnBackdrop } from '../components/modal';
import { getUsers, createUser, updateUser, changePassword } from '../api/users.api';
import { escHtml, escAttr } from '../utils/escHtml';
import { ApiError } from '../api/client';
import { currentUser } from '../auth';
import type { UserRecord } from '../types';

let _users: UserRecord[] = [];
let _editingId: string | null = null;

export async function renderAdminUsers(): Promise<void> {
  renderLayout(spinner());

  try {
    _users = await getUsers();
  } catch {
    document.getElementById('page-content')!.innerHTML = errorMessage('Failed to load users.');
    return;
  }

  renderPage();
  attachEvents();
}

function renderPage(): void {
  const content = document.getElementById('page-content')!;
  content.innerHTML = `
    <div class="bh-page-header">
      <h2 class="bh-page-title">Users</h2>
      <button id="btn-add-user" class="bh-btn bh-btn-primary">Add User</button>
    </div>
    ${_users.length === 0
      ? emptyState('No users found.')
      : `<div class="bh-table-scroll">
          <table class="bh-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Username</th>
                <th>Role</th>
                <th>Status</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              ${_users.map(u => userRow(u)).join('')}
            </tbody>
          </table>
        </div>`
    }

    <div id="modal-user" class="bh-modal" onclick="window._closeUserModalOnBackdrop(event)">
      <div class="bh-modal-inner">
        <h3 class="bh-modal-title" id="modal-user-title">Add User</h3>
        <form id="user-form" class="bh-form" novalidate>
          <div class="bh-form-grid bh-form-grid-2">
            <div class="bh-form-group">
              <label for="user-first-name" class="bh-label">First Name</label>
              <input id="user-first-name" type="text" class="bh-input" maxlength="100" required />
            </div>
            <div class="bh-form-group">
              <label for="user-last-name" class="bh-label">Last Name</label>
              <input id="user-last-name" type="text" class="bh-input" maxlength="100" required />
            </div>
          </div>
          <div id="user-username-group" class="bh-form-group">
            <label for="user-username" class="bh-label">Username</label>
            <input id="user-username" type="text" class="bh-input" maxlength="100" required />
          </div>
          <div id="user-password-group" class="bh-form-group">
            <label for="user-password" class="bh-label">Password</label>
            <input id="user-password" type="password" class="bh-input" maxlength="200" />
          </div>
          <div class="bh-form-grid bh-form-grid-2">
            <div class="bh-form-group">
              <label for="user-role" class="bh-label">Role</label>
              <select id="user-role" class="bh-input">
                <option value="User">User</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
            <div id="user-active-group" class="bh-form-group" style="display:flex;align-items:center;gap:0.5rem;padding-top:1.75rem">
              <input id="user-active" type="checkbox" style="width:1rem;height:1rem" />
              <label for="user-active" class="bh-label" style="margin:0">Active</label>
            </div>
          </div>
          <div id="user-form-error" class="bh-error-message" style="display:none"></div>
          <div style="display:flex;gap:0.75rem;justify-content:flex-end;margin-top:0.5rem">
            <button type="button" id="btn-cancel-user" class="bh-btn">Cancel</button>
            <button type="submit" id="btn-save-user" class="bh-btn bh-btn-primary">Save</button>
          </div>
        </form>
      </div>
    </div>

    <div id="modal-password" class="bh-modal" onclick="window._closePasswordModalOnBackdrop(event)">
      <div class="bh-modal-inner">
        <h3 class="bh-modal-title">Change Password</h3>
        <form id="password-form" class="bh-form" novalidate>
          <div id="current-password-group" class="bh-form-group">
            <label for="current-password" class="bh-label">Current Password</label>
            <input id="current-password" type="password" class="bh-input" maxlength="200" />
          </div>
          <div class="bh-form-group">
            <label for="new-password" class="bh-label">New Password</label>
            <input id="new-password" type="password" class="bh-input" maxlength="200" required />
          </div>
          <div id="password-form-error" class="bh-error-message" style="display:none"></div>
          <div style="display:flex;gap:0.75rem;justify-content:flex-end;margin-top:0.5rem">
            <button type="button" id="btn-cancel-password" class="bh-btn">Cancel</button>
            <button type="submit" id="btn-save-password" class="bh-btn bh-btn-primary">Save</button>
          </div>
        </form>
      </div>
    </div>
  `;
}

function userRow(u: UserRecord): string {
  const statusBadge = u.isActive
    ? '<span class="bh-badge bh-badge-active">Active</span>'
    : '<span class="bh-badge bh-badge-inactive">Inactive</span>';
  return `<tr>
    <td>${escHtml(u.firstName)} ${escHtml(u.lastName)}</td>
    <td>${escHtml(u.username)}</td>
    <td>${roleBadge(u.role)}</td>
    <td>${statusBadge}</td>
    <td style="text-align:right;white-space:nowrap">
      <button class="bh-btn bh-btn-sm" data-edit="${escAttr(u.id)}">Edit</button>
      <button class="bh-btn bh-btn-sm" data-pw="${escAttr(u.id)}" style="margin-left:0.5rem">Password</button>
    </td>
  </tr>`;
}

function attachEvents(): void {
  (window as unknown as Record<string, unknown>)._closeUserModalOnBackdrop =
    (e: MouseEvent) => closeModalOnBackdrop('modal-user', e);
  (window as unknown as Record<string, unknown>)._closePasswordModalOnBackdrop =
    (e: MouseEvent) => closeModalOnBackdrop('modal-password', e);

  document.getElementById('btn-add-user')?.addEventListener('click', () => openAddModal());
  document.getElementById('btn-cancel-user')?.addEventListener('click', () => closeModal('modal-user'));
  document.getElementById('user-form')?.addEventListener('submit', handleUserFormSubmit);

  document.getElementById('btn-cancel-password')?.addEventListener('click', () => closeModal('modal-password'));
  document.getElementById('password-form')?.addEventListener('submit', handlePasswordFormSubmit);

  document.querySelectorAll('[data-edit]').forEach(btn => {
    btn.addEventListener('click', () => {
      const id = (btn as HTMLElement).dataset['edit']!;
      const user = _users.find(u => u.id === id);
      if (user) openEditModal(user);
    });
  });

  document.querySelectorAll('[data-pw]').forEach(btn => {
    btn.addEventListener('click', () => {
      const id = (btn as HTMLElement).dataset['pw']!;
      openPasswordModal(id);
    });
  });
}

function openAddModal(): void {
  _editingId = null;
  (document.getElementById('modal-user-title') as HTMLElement).textContent = 'Add User';
  (document.getElementById('user-first-name') as HTMLInputElement).value = '';
  (document.getElementById('user-last-name') as HTMLInputElement).value = '';
  (document.getElementById('user-username') as HTMLInputElement).value = '';
  (document.getElementById('user-password') as HTMLInputElement).value = '';
  (document.getElementById('user-role') as HTMLSelectElement).value = 'User';
  (document.getElementById('user-active') as HTMLInputElement).checked = true;
  (document.getElementById('user-username-group') as HTMLElement).style.display = '';
  (document.getElementById('user-password-group') as HTMLElement).style.display = '';
  (document.getElementById('user-active-group') as HTMLElement).style.display = 'none';
  (document.getElementById('user-form-error') as HTMLElement).style.display = 'none';
  openModal('modal-user');
}

function openEditModal(user: UserRecord): void {
  _editingId = user.id;
  (document.getElementById('modal-user-title') as HTMLElement).textContent = 'Edit User';
  (document.getElementById('user-first-name') as HTMLInputElement).value = user.firstName;
  (document.getElementById('user-last-name') as HTMLInputElement).value = user.lastName;
  (document.getElementById('user-role') as HTMLSelectElement).value = user.role;
  (document.getElementById('user-active') as HTMLInputElement).checked = user.isActive;
  (document.getElementById('user-username-group') as HTMLElement).style.display = 'none';
  (document.getElementById('user-password-group') as HTMLElement).style.display = 'none';
  (document.getElementById('user-active-group') as HTMLElement).style.display = '';
  (document.getElementById('user-form-error') as HTMLElement).style.display = 'none';
  openModal('modal-user');
}

let _passwordTargetId: string | null = null;

function openPasswordModal(userId: string): void {
  _passwordTargetId = userId;
  const me = currentUser();
  const isOwnAccount = me?.id === userId;
  (document.getElementById('current-password-group') as HTMLElement).style.display = isOwnAccount ? '' : 'none';
  (document.getElementById('current-password') as HTMLInputElement).value = '';
  (document.getElementById('new-password') as HTMLInputElement).value = '';
  (document.getElementById('password-form-error') as HTMLElement).style.display = 'none';
  openModal('modal-password');
}

async function handleUserFormSubmit(e: Event): Promise<void> {
  e.preventDefault();
  const errorEl = document.getElementById('user-form-error') as HTMLElement;
  const saveBtn = document.getElementById('btn-save-user') as HTMLButtonElement;
  errorEl.style.display = 'none';
  saveBtn.disabled = true;

  const firstName = (document.getElementById('user-first-name') as HTMLInputElement).value.trim();
  const lastName = (document.getElementById('user-last-name') as HTMLInputElement).value.trim();
  const role = (document.getElementById('user-role') as HTMLSelectElement).value;

  try {
    if (_editingId) {
      const isActive = (document.getElementById('user-active') as HTMLInputElement).checked;
      const updated = await updateUser(_editingId, { firstName, lastName, role, isActive });
      _users = _users.map(u => u.id === _editingId ? updated : u);
    } else {
      const username = (document.getElementById('user-username') as HTMLInputElement).value.trim();
      const password = (document.getElementById('user-password') as HTMLInputElement).value;
      const created = await createUser({ firstName, lastName, username, password, role });
      _users = [..._users, created];
    }
    closeModal('modal-user');
    renderPage();
    attachEvents();
  } catch (err) {
    const msg = err instanceof ApiError ? parseApiError(err) : 'An error occurred. Please try again.';
    errorEl.textContent = msg;
    errorEl.style.display = 'block';
    saveBtn.disabled = false;
  }
}

async function handlePasswordFormSubmit(e: Event): Promise<void> {
  e.preventDefault();
  const errorEl = document.getElementById('password-form-error') as HTMLElement;
  const saveBtn = document.getElementById('btn-save-password') as HTMLButtonElement;
  errorEl.style.display = 'none';
  saveBtn.disabled = true;

  const me = currentUser();
  const isOwnAccount = me?.id === _passwordTargetId;
  const currentPassword = isOwnAccount
    ? (document.getElementById('current-password') as HTMLInputElement).value
    : undefined;
  const newPassword = (document.getElementById('new-password') as HTMLInputElement).value;

  try {
    await changePassword(_passwordTargetId!, { currentPassword, newPassword });
    closeModal('modal-password');
  } catch (err) {
    const msg = err instanceof ApiError ? parseApiError(err) : 'An error occurred. Please try again.';
    errorEl.textContent = msg;
    errorEl.style.display = 'block';
    saveBtn.disabled = false;
  }
}

function parseApiError(err: ApiError): string {
  try {
    const body = JSON.parse(err.message);
    return body.detail ?? 'Validation failed.';
  } catch {
    return err.message;
  }
}

const STORAGE_KEY = 'golfclub-admin-key';

export function getAdminKey(): string {
  return localStorage.getItem(STORAGE_KEY) ?? '';
}

export function setAdminKey(key: string): void {
  localStorage.setItem(STORAGE_KEY, key);
}

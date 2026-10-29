import type { LoginResponse } from '../api/types';

const storageKey = 'logistiflow.session';

/**
 * The access token lives in sessionStorage: it survives a reload but not the tab, and is never written to a
 * cookie. Trade-off (see ADR-0004): a BFF with an HttpOnly cookie would hide the token from scripts entirely.
 */
export function readSession(): LoginResponse | null {
  try {
    const raw = sessionStorage.getItem(storageKey);
    if (!raw) {
      return null;
    }

    const session = JSON.parse(raw) as LoginResponse;
    return new Date(session.expiresAt).getTime() > Date.now() ? session : null;
  } catch {
    return null;
  }
}

export function writeSession(session: LoginResponse): void {
  sessionStorage.setItem(storageKey, JSON.stringify(session));
}

export function clearSession(): void {
  sessionStorage.removeItem(storageKey);
}

type Listener = () => void;
const expiredListeners = new Set<Listener>();

/** Notifies the auth provider when the API rejects the token, so the UI returns to the login page. */
export function onSessionExpired(listener: Listener): () => void {
  expiredListeners.add(listener);
  return () => expiredListeners.delete(listener);
}

export function notifySessionExpired(): void {
  clearSession();
  expiredListeners.forEach((listener) => listener());
}

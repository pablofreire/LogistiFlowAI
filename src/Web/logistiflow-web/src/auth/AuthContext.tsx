import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { login as loginRequest } from '../api/hooks';
import type { AuthenticatedUser, Role } from '../api/types';
import { clearSession, onSessionExpired, readSession, writeSession } from './session';

export interface AuthState {
  user: AuthenticatedUser | null;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => void;
  hasRole: (...roles: Role[]) => boolean;
}

export const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(() => readSession()?.user ?? null);

  useEffect(() => onSessionExpired(() => setUser(null)), []);

  const signIn = useCallback(async (email: string, password: string) => {
    const { data } = await loginRequest(email, password);
    writeSession(data);
    setUser(data.user);
  }, []);

  const signOut = useCallback(() => {
    clearSession();
    setUser(null);
  }, []);

  const hasRole = useCallback((...roles: Role[]) => user !== null && roles.includes(user.role as Role), [user]);

  const value = useMemo(() => ({ user, signIn, signOut, hasRole }), [user, signIn, signOut, hasRole]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

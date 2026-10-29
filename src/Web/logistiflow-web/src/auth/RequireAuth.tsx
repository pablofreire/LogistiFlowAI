import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import type { Role } from '../api/types';
import { AccessDenied } from '../components/states/AccessDenied';
import { useAuth } from './useAuth';

/** Redirects anonymous users to the login page; shows "access denied" when the role is not allowed. */
export function RequireAuth({ children, roles }: { children: ReactNode; roles?: Role[] }) {
  const { user, hasRole } = useAuth();
  const location = useLocation();

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (roles && !hasRole(...roles)) {
    return <AccessDenied />;
  }

  return children;
}

import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const requiredRoles: string[] = route.data['roles'] ?? [];

  if (!auth.getAccessToken()) {
    router.navigate(['/login']);
    return false;
  }

  if (requiredRoles.length === 0) return true;

  const hasRole = requiredRoles.some(r => auth.hasRole(r));
  if (!hasRole) {
    router.navigate(['/unauthorized']);
    return false;
  }

  return true;
};

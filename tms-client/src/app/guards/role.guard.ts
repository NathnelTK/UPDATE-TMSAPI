import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * M11 Session 3 - Exercise 6 Step 2: functional role guard factory.
 *
 * Returns a CanActivateFn that admits the route only when the current user holds
 * `requiredRole` (AuthService.hasRole also grants Admins everything). Anyone else
 * is redirected to /unauthorized rather than /login — they ARE signed in, they
 * simply lack the role, so bouncing them to the login page would be misleading.
 *
 * Usage: `canActivate: [roleGuard('Admin')]`.
 */
export const roleGuard = (requiredRole: string): CanActivateFn => {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url },
      });
    }

    if (auth.hasRole(requiredRole)) {
      return true;
    }

    return router.createUrlTree(['/unauthorized']);
  };
};

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError, Observable, BehaviorSubject, filter, take } from 'rxjs';
import { AuthService, TokenPair } from '../services/auth.service';

// Single-flight refresh: if several requests 401 at once, only the first triggers
// a token refresh; the rest park on this subject until the new token lands.
let refreshing = false;
const refreshed$ = new BehaviorSubject<string | null>(null);

/** Requests to these endpoints must never carry a stale bearer token or trigger a retry loop. */
function isAuthEndpoint(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/refresh');
}

function withBearer(req: Parameters<HttpInterceptorFn>[0], token: string) {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

/**
 * Attaches `Authorization: Bearer <accessToken>` to API calls and transparently
 * recovers from an expired access token: on the first 401 it rotates the refresh
 * token once and replays the original request; a second failure (or a failed
 * refresh) logs the user out.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Login/refresh calls go out untouched.
  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  const token = auth.accessToken;
  const authReq = token ? withBearer(req, token) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // Only a 401 on an authenticated request is worth a refresh attempt.
      if (err.status !== 401 || !token) {
        return throwError(() => err);
      }
      return handle401(req, next, auth);
    }),
  );
};

function handle401(
  req: Parameters<HttpInterceptorFn>[0],
  next: Parameters<HttpInterceptorFn>[1],
  auth: AuthService,
): Observable<import('@angular/common/http').HttpEvent<unknown>> {
  if (refreshing) {
    // A refresh is already in flight — wait for the new token, then replay.
    return refreshed$.pipe(
      filter((t): t is string => t !== null),
      take(1),
      switchMap((newToken) => next(withBearer(req, newToken))),
    );
  }

  refreshing = true;
  refreshed$.next(null);

  return auth.refresh().pipe(
    switchMap((tokens: TokenPair) => {
      refreshing = false;
      refreshed$.next(tokens.accessToken);
      return next(withBearer(req, tokens.accessToken));
    }),
    catchError((refreshErr) => {
      refreshing = false;
      auth.logout();
      return throwError(() => refreshErr);
    }),
  );
}

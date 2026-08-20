import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

// Global HTTP error handling. Parses the C# RFC 7807 ProblemDetails "detail" field
// so the UI/console shows a meaningful message instead of Angular's opaque
// "Http failure response for (unknown url): 0 Unknown Error".
//
// NOTE: 401 handling deliberately lives in authInterceptor now (refresh + retry,
// then logout). This interceptor no longer redirects on 401 so the two don't race.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Extract the C# RFC 7807 ProblemDetails detail property (falls back to a
      // generic message for network failures / non-Problem responses).
      const detailMessage =
        err.error?.detail ?? err.message ?? 'A system error occurred. Please try again.';

      // Surface the structured error to the developer console; components read
      // `err.error.detail` themselves to render inline messages.
      console.error(`API Error (${err.status}):`, detailMessage);

      return throwError(() => err);
    }),
  );
};

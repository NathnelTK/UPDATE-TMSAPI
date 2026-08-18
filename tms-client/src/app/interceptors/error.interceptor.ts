import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

// M10 Session 3 - Exercise 3 Part A: global HTTP error handling.
// Parses the C# RFC 7807 ProblemDetails "detail" field so the UI/console shows a
// meaningful message instead of Angular's opaque "Http failure response for
// (unknown url): 0 Unknown Error", and bounces expired/unauthenticated sessions
// back to /login.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Extract the C# RFC 7807 ProblemDetails detail property.
      const detailMessage =
        err.error?.detail ?? 'A system error occurred. Please try again.';

      if (err.status === 401) {
        // Redirect expired or unauthenticated sessions back to login.
        router.navigate(['/login']);
      } else {
        // Surface the structured error to the developer console / UI notification.
        console.error('API Error Response:', detailMessage);
      }

      return throwError(() => err);
    }),
  );
};

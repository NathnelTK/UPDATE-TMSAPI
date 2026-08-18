import { HttpInterceptorFn } from '@angular/common/http';

// M10 Session 2 - Exercise 2 Part C: attach credentials to every request.
// By default Angular's HttpClient omits cookies on cross-origin requests, so the
// browser would never send the HttpOnly tms_auth cookie to the API on :7190.
// Cloning each request with withCredentials:true opts the whole app into sending
// (and receiving Set-Cookie for) credentials, which is what the "TmsClient" CORS
// policy's AllowCredentials() on the server permits.
export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req.clone({ withCredentials: true }));
};

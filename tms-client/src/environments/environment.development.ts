// Development environment (swapped in for `ng serve` and dev builds).
// The Angular dev server runs on http://localhost:4200 while the .NET API
// listens on http://localhost:5003 (the default `dotnet run` "http" profile) — a
// different origin. Absolute URLs make the
// browser issue genuine cross-origin requests, which the API's "TmsClient" CORS
// policy grants.
//
//   apiBase — root of the API (unversioned)
//   apiUrl  — versioned data endpoints (courses, enrollments, students)
//   authUrl — JWT auth endpoints (login/refresh/register), which are NOT versioned
export const environment = {
  production: false,
  apiBase: 'http://localhost:5003/api',
  apiUrl: 'http://localhost:5003/api/v2',
  authUrl: 'http://localhost:5003/api/auth',
};

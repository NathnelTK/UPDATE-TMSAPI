// Development environment (swapped in for `ng serve` and dev builds).
// The Angular dev server runs on http://localhost:4200 while the .NET API
// listens on https://localhost:7190 — a different origin. Pointing apiUrl at
// the absolute backend URL is what makes the browser issue a genuine
// cross-origin request, which the API's named "TmsClient" CORS policy grants.
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7190/api/v2',
};

// Production environment (base file imported everywhere).
// `ng build` uses this file as-is; `ng serve` / development builds swap in
// environment.development.ts via the fileReplacements entry in angular.json.
// URLs are relative here so a production deployment can serve API + client from
// the same origin (no CORS needed once co-hosted behind one domain).
export const environment = {
  production: true,
  apiBase: '/api',
  apiUrl: '/api/v2',
  authUrl: '/api/auth',
};

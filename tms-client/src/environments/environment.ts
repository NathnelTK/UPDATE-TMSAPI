// Production environment (base file imported everywhere).
// `ng build` uses this file as-is; `ng serve` / development builds swap in
// environment.development.ts via the fileReplacements entry in angular.json.
// apiUrl is relative here so a production deployment can serve API + client
// from the same origin (no CORS needed once co-hosted behind one domain).
export const environment = {
  production: true,
  apiUrl: '/api/v2',
};

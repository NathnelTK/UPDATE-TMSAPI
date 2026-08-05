import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/student-dashboard/student-dashboard.component').then(
        (m) => m.StudentDashboardComponent,
      ),
  },
  // Redirect root URL to /dashboard
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];

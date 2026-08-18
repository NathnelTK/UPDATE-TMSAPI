import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/student-dashboard/student-dashboard.component').then(
        (m) => m.StudentDashboardComponent,
      ),
  },
  {
    path: 'courses/:id',
    loadComponent: () =>
      import('./features/course-detail/course-detail.component').then(
        (m) => m.CourseDetailComponent,
      ),
  },
  {
    path: 'enroll',
    loadComponent: () =>
      import('./features/enrollment-form/enrollment-form.component').then(
        (m) => m.EnrollmentFormComponent,
      ),
  },
  {
    // M9 Session 1 — Enrollment Command Center with SignalStore
    path: 'enrollments',
    loadComponent: () =>
      import('./features/enrollment-command-center/enrollment-command-center.component').then(
        (m) => m.EnrollmentCommandCenterComponent,
      ),
  },
  {
    // M9 Session 2 — Instructor dashboard with a @defer-loaded analytics chart.
    // loadComponent gives route-level code splitting; the @defer block inside
    // the template adds a second layer that loads the chart chunk on viewport.
    path: 'instructor-dashboard',
    loadComponent: () =>
      import('./features/instructor-dashboard/instructor-dashboard.component').then(
        (m) => m.InstructorDashboardComponent,
      ),
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];

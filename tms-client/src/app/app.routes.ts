import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  // Public sign-in page — rendered outside the authenticated shell.
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent),
  },

  // Everything else lives inside the shell and requires a valid session.
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
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
        // Enrollment Command Center with SignalStore-backed approval queue.
        path: 'enrollments',
        loadComponent: () =>
          import(
            './features/enrollment-command-center/enrollment-command-center.component'
          ).then((m) => m.EnrollmentCommandCenterComponent),
      },
      {
        // Instructor dashboard with a @defer-loaded analytics chart.
        path: 'instructor-dashboard',
        loadComponent: () =>
          import('./features/instructor-dashboard/instructor-dashboard.component').then(
            (m) => m.InstructorDashboardComponent,
          ),
      },
      {
        // Student schedule — GET /api/v2/enrollments/{studentId}/schedule.
        path: 'schedule',
        loadComponent: () =>
          import('./features/schedule/schedule.component').then(
            (m) => m.ScheduleComponent,
          ),
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },

  // Unknown URLs fall back to the shell (which the guard will gate).
  { path: '**', redirectTo: '' },
];

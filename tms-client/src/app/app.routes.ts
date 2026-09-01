import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell.component';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';

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
      {
        // M11 Session 3 - Exercise 6: admin-only area, gated by roleGuard('Admin').
        // Non-admins are redirected to /unauthorized by the guard before load.
        path: 'admin/courses',
        canActivate: [roleGuard('Admin')],
        loadComponent: () =>
          import('./features/admin-courses/admin-courses.component').then(
            (m) => m.AdminCoursesComponent,
          ),
      },
      {
        // M11 Session 3 - Exercise 6: where roleGuard sends users who lack the role.
        path: 'unauthorized',
        loadComponent: () =>
          import('./features/unauthorized/unauthorized.component').then(
            (m) => m.UnauthorizedComponent,
          ),
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },

  // Unknown URLs fall back to the shell (which the guard will gate).
  { path: '**', redirectTo: '' },
];

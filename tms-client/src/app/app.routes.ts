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

  // The shell and catalog are public. Mutating or personal workflows add their
  // own guard so visitors can explore before being asked to sign in.
  {
    path: '',
    component: ShellComponent,
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/student-dashboard/student-dashboard.component').then(
            (m) => m.StudentDashboardComponent,
        ),
      },
      { path: 'attendance', canActivate: [authGuard], loadComponent: () => import('./features/learning-records/learning-records.component').then((m) => m.LearningRecordsComponent) },
      { path: 'courses', loadComponent: () => import('./features/course-catalog/course-catalog.component').then((m) => m.CourseCatalogComponent) },
      { path: 'enrollments', canActivate: [authGuard], loadComponent: () => import('./features/student-enrollments/student-enrollments.component').then((m) => m.StudentEnrollmentsComponent) },
      { path: 'grades', canActivate: [authGuard], loadComponent: () => import('./features/learning-records/grades.component').then((m) => m.GradesComponent) },
      { path: 'grants', canActivate: [authGuard], loadComponent: () => import('./features/learning-records/grants.component').then((m) => m.GrantsComponent) },
      { path: 'notifications', canActivate: [authGuard], loadComponent: () => import('./features/learning-records/notifications.component').then((m) => m.NotificationsComponent) },
      {
        path: 'courses/:id',
        loadComponent: () =>
          import('./features/course-detail/course-detail.component').then(
            (m) => m.CourseDetailComponent,
          ),
      },
      {
        path: 'enroll',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/enrollment-form/enrollment-form.component').then(
            (m) => m.EnrollmentFormComponent,
          ),
      },
      {
        // Enrollment Command Center with SignalStore-backed approval queue.
        path: 'admin/enrollments',
        canActivate: [authGuard],
        loadComponent: () =>
          import(
            './features/enrollment-command-center/enrollment-command-center.component'
          ).then((m) => m.EnrollmentCommandCenterComponent),
      },
      {
        // Instructor dashboard with a @defer-loaded analytics chart.
        path: 'instructor-dashboard',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/instructor-dashboard/instructor-dashboard.component').then(
            (m) => m.InstructorDashboardComponent,
          ),
      },
      {
        // Student schedule — GET /api/v2/enrollments/{studentId}/schedule.
        path: 'schedule',
        canActivate: [authGuard],
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
      { path: 'admin/attendance', canActivate: [roleGuard('Admin')], loadComponent: () => import('./features/learning-records/learning-records.component').then((m) => m.LearningRecordsComponent) },
      { path: 'admin/grades', canActivate: [roleGuard('Admin')], loadComponent: () => import('./features/learning-records/grades.component').then((m) => m.GradesComponent) },
      { path: 'admin/grants', canActivate: [roleGuard('Admin')], loadComponent: () => import('./features/learning-records/grants.component').then((m) => m.GrantsComponent) },
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

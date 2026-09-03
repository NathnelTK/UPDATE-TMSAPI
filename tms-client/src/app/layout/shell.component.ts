import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ThemeService } from '../services/theme.service';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

/**
 * Authenticated app shell: fixed sidebar (brand + primary nav) and a topbar
 * (page title, theme toggle, user identity + logout). Hosts the routed views
 * through <router-outlet>. Collapses to an off-canvas drawer on small screens.
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private theme = inject(ThemeService);

  readonly studentNav: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/courses', label: 'Course Catalog', icon: 'menu_book' },
    { path: '/enrollments', label: 'My Enrollments', icon: 'fact_check' },
    { path: '/schedule', label: 'My Schedule', icon: 'calendar_month' },
    { path: '/attendance', label: 'Attendance', icon: 'event_available' },
    { path: '/grades', label: 'Grades', icon: 'grade' },
    { path: '/grants', label: 'Grants', icon: 'account_balance' },
    { path: '/notifications', label: 'Notifications', icon: 'notifications' },
  ];
  readonly adminNav: NavItem[] = [
    { path: '/dashboard', label: 'Overview', icon: 'dashboard' },
    { path: '/admin/courses', label: 'Courses', icon: 'menu_book' },
    { path: '/admin/enrollments', label: 'Enrollment Requests', icon: 'fact_check' },
    { path: '/admin/attendance', label: 'Attendance', icon: 'event_available' },
    { path: '/admin/grades', label: 'Assessments & Grades', icon: 'grade' },
    { path: '/admin/grants', label: 'Grant Applications', icon: 'account_balance' },
  ];

  readonly user = this.auth.currentUser;
  // M11 Session 3 - Exercise 6 Step 3: drives the conditional Admin nav entry.
  readonly isAdmin = computed(() => this.auth.hasRole('Admin'));
  readonly visibleNav = computed(() => this.isAdmin() ? this.adminNav : this.studentNav);
  readonly isDark = computed(() => this.theme.theme() === 'dark');
  readonly pageTitle = signal(this.titleFor(this.router.url));
  readonly drawerOpen = signal(false);

  readonly initials = computed(() => {
    const name = this.user()?.displayName ?? '';
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  });

  constructor() {
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((e) => {
        this.pageTitle.set(this.titleFor(e.urlAfterRedirects));
        this.drawerOpen.set(false); // auto-close the mobile drawer on navigation
      });
  }

  toggleTheme(): void {
    this.theme.toggle();
  }

  toggleDrawer(): void {
    this.drawerOpen.update((v) => !v);
  }

  logout(): void {
    this.auth.logout();
  }

  private titleFor(url: string): string {
    const match = [...this.studentNav, ...this.adminNav].find((n) => url.startsWith(n.path));
    if (match) return match.label;
    if (url.startsWith('/courses')) return 'Course Detail';
    if (url.startsWith('/admin')) return 'Administration';
    if (url.startsWith('/unauthorized')) return 'Access denied';
    return 'Dashboard';
  }
}

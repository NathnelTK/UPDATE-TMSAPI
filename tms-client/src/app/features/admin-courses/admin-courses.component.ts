import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { CourseStore } from '../../store/course.store';

/**
 * M11 Session 3 - Exercise 6: admin-only course management view.
 *
 * Reachable at /admin/courses, which is gated by roleGuard('Admin') — non-admins
 * never reach this component; they're redirected to /unauthorized first. It reuses
 * the existing CourseStore (optimistic delete + rollback) and demonstrates Step 3's
 * conditional UI: the destructive "Delete" action is wrapped in
 * `@if (auth.hasRole('Admin'))` as defence-in-depth behind the route guard.
 */
@Component({
  selector: 'app-admin-courses',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="admin">
      <header class="admin-head">
        <h1>Course administration</h1>
        <p class="sub">Manage the course catalogue. Admin access only.</p>
      </header>

      @if (store.error(); as err) {
        <div class="banner error">{{ err }}</div>
      }

      @if (store.isLoading()) {
        <p class="muted">Loading courses…</p>
      } @else {
        <table class="grid">
          <thead>
            <tr>
              <th>Code</th>
              <th>Title</th>
              <th class="num">Capacity</th>
              <th class="num">Enrolled</th>
              <th aria-label="Actions"></th>
            </tr>
          </thead>
          <tbody>
            @for (course of store.entities(); track course.id) {
              <tr>
                <td class="code">{{ course.code }}</td>
                <td>{{ course.title }}</td>
                <td class="num">{{ course.maxCapacity }}</td>
                <td class="num">{{ course.enrollmentCount }}</td>
                <td class="actions">
                  @if (auth.hasRole('Admin')) {
                    <button type="button" class="btn-danger" (click)="delete(course.id)">
                      <span class="material-icons">delete</span>
                      Delete
                    </button>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5" class="muted">No courses found.</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: [
    `
      .admin {
        display: flex;
        flex-direction: column;
        gap: 1.25rem;
      }
      .admin-head h1 {
        margin: 0;
        font-size: 1.5rem;
      }
      .sub {
        margin: 0.25rem 0 0;
        color: var(--text-muted, #6b7280);
      }
      .banner.error {
        padding: 0.75rem 1rem;
        border-radius: 0.5rem;
        background: color-mix(in srgb, #dc2626 12%, transparent);
        color: #b91c1c;
        border: 1px solid color-mix(in srgb, #dc2626 30%, transparent);
      }
      .grid {
        width: 100%;
        border-collapse: collapse;
        background: var(--surface, #fff);
        border-radius: 0.75rem;
        overflow: hidden;
        box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
      }
      .grid th,
      .grid td {
        padding: 0.75rem 1rem;
        text-align: left;
        border-bottom: 1px solid var(--border, #e5e7eb);
      }
      .grid th {
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--text-muted, #6b7280);
      }
      .grid tr:last-child td {
        border-bottom: none;
      }
      .num {
        text-align: right;
        font-variant-numeric: tabular-nums;
      }
      .code {
        font-weight: 600;
      }
      .actions {
        text-align: right;
      }
      .muted {
        color: var(--text-muted, #6b7280);
      }
      .btn-danger {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        padding: 0.4rem 0.75rem;
        border: none;
        border-radius: 0.5rem;
        background: #dc2626;
        color: #fff;
        font-weight: 600;
        cursor: pointer;
      }
      .btn-danger:hover {
        filter: brightness(1.05);
      }
      .btn-danger .material-icons {
        font-size: 1.1rem;
      }
    `,
  ],
})
export class AdminCoursesComponent implements OnInit {
  protected readonly store = inject(CourseStore);
  protected readonly auth = inject(AuthService);

  ngOnInit(): void {
    this.store.loadCourses();
  }

  delete(id: number): void {
    this.store.deleteCourse(id);
  }
}

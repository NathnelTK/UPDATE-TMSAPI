import { Component, inject } from '@angular/core';
import { EnrollmentStore } from '../../store/enrollment.store';

/**
 * Dashboard Summary widget — reads pendingCount() from the same singleton
 * EnrollmentStore. When approveEnrollment() fires in EnrollmentListComponent,
 * this widget's count drops instantly with no extra API call. This is the
 * demonstration that single-source-of-truth eliminates state drift.
 */
@Component({
  selector: 'tms-dashboard-summary',
  standalone: true,
  template: `
    <div class="summary-card">
      <h3>Pending Enrollments</h3>
      <p class="count">{{ store.pendingCount() }}</p>
    </div>
  `,
  styles: [`
    .summary-card {
      padding: 1.5rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      text-align: center;
      min-width: 160px;

      h3 { margin: 0 0 0.5rem; font-size: 0.9rem; color: #555; }
      .count { font-size: 2.5rem; font-weight: 700; color: #1976d2; margin: 0; }
    }
  `],
})
export class DashboardSummaryComponent {
  store = inject(EnrollmentStore);
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { EnrollmentStore } from '../../store/enrollment.store';

/**
 * Dashboard Summary widget — reads derived counts from the same singleton
 * EnrollmentStore as the queue table. Approving/rejecting in the list updates
 * these figures instantly with no extra API call (single source of truth).
 */
@Component({
  selector: 'tms-dashboard-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="summary">
      <h3 class="summary-title">At a glance</h3>

      <div class="stat">
        <span class="stat-icon i-pending material-icons">pending_actions</span>
        <div class="stat-body">
          <span class="stat-value">{{ store.pendingCount() }}</span>
          <span class="stat-label">Pending</span>
        </div>
      </div>

      <div class="stat">
        <span class="stat-icon i-approved material-icons">task_alt</span>
        <div class="stat-body">
          <span class="stat-value">{{ store.approvedCount() }}</span>
          <span class="stat-label">Approved</span>
        </div>
      </div>

      <div class="stat">
        <span class="stat-icon i-total material-icons">groups</span>
        <div class="stat-body">
          <span class="stat-value">{{ store.total() }}</span>
          <span class="stat-label">Total records</span>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .summary {
        background: var(--tms-surface);
        border: 1px solid var(--tms-border);
        border-radius: var(--tms-radius-lg);
        box-shadow: var(--tms-shadow-sm);
        padding: 1.25rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      .summary-title {
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: var(--tms-text-faint);
        margin: 0;
      }
      .stat {
        display: flex;
        align-items: center;
        gap: 0.75rem;
      }
      .stat-icon {
        display: grid;
        place-items: center;
        width: 40px;
        height: 40px;
        border-radius: var(--tms-radius-md);
        font-size: 21px;
      }
      .i-pending { background: var(--tms-warning-weak); color: var(--tms-warning); }
      .i-approved { background: var(--tms-success-weak); color: var(--tms-success); }
      .i-total { background: var(--tms-primary-weak); color: var(--tms-primary); }
      .stat-value {
        display: block;
        font-size: 1.5rem;
        font-weight: 700;
        line-height: 1.1;
      }
      .stat-label {
        font-size: 0.8rem;
        color: var(--tms-text-muted);
      }
    `,
  ],
})
export class DashboardSummaryComponent {
  store = inject(EnrollmentStore);
}

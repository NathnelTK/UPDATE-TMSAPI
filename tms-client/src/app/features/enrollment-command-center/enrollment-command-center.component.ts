import { Component } from '@angular/core';
import { EnrollmentListComponent } from '../enrollment-list/enrollment-list.component';
import { DashboardSummaryComponent } from '../dashboard-summary/dashboard-summary.component';

/**
 * Hosts the EnrollmentList and DashboardSummary side-by-side.
 * Both inject the same singleton EnrollmentStore — approving an enrollment
 * in the list drops the pendingCount in the summary widget instantly,
 * with no page refresh and no duplicate API call.
 */
@Component({
  selector: 'app-enrollment-command-center',
  standalone: true,
  imports: [EnrollmentListComponent, DashboardSummaryComponent],
  template: `
    <div class="command-center">
      <div class="sidebar">
        <tms-dashboard-summary />
      </div>
      <div class="main">
        <h2>Enrollment Queue</h2>
        <tms-enrollment-list />
      </div>
    </div>
  `,
  styles: [`
    .command-center {
      display: flex;
      gap: 2rem;
      padding: 2rem;
      max-width: 1000px;
      margin: 0 auto;
    }
    .sidebar { flex: 0 0 180px; }
    .main    { flex: 1; }
    h2       { margin-top: 0; }
  `],
})
export class EnrollmentCommandCenterComponent {}

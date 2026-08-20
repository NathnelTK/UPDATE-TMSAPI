import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EnrollmentListComponent } from '../enrollment-list/enrollment-list.component';
import { DashboardSummaryComponent } from '../dashboard-summary/dashboard-summary.component';

/**
 * Hosts the EnrollmentList and DashboardSummary side-by-side. Both inject the
 * same singleton EnrollmentStore — approving/rejecting in the list updates the
 * summary counts instantly, with no page refresh and no duplicate API call.
 */
@Component({
  selector: 'app-enrollment-command-center',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EnrollmentListComponent, DashboardSummaryComponent],
  template: `
    <header class="cc-head">
      <h1>Enrollment queue</h1>
      <p class="tms-muted">Review pending requests and approve or reject them.</p>
    </header>

    <div class="command-center">
      <aside class="sidebar">
        <tms-dashboard-summary />
      </aside>
      <section class="main">
        <tms-enrollment-list />
      </section>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .cc-head { margin-bottom: 1.5rem; }
      .cc-head h1 { font-size: 1.6rem; margin: 0 0 0.25rem; }
      .cc-head p { margin: 0; }
      .command-center {
        display: grid;
        grid-template-columns: 240px 1fr;
        gap: 1.5rem;
        align-items: start;
      }
      @media (max-width: 820px) {
        .command-center { grid-template-columns: 1fr; }
      }
    `,
  ],
})
export class EnrollmentCommandCenterComponent {}

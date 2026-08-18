import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AnalyticsChartComponent } from '../../ui/analytics-chart/analytics-chart.component';

/**
 * Instructor Command Center page.
 *
 * The critical UI (enrollment count, pending approvals) renders immediately.
 * The heavy AnalyticsChartComponent is used only inside a @defer block, so the
 * compiler splits it into a separate JS chunk that downloads on viewport/idle —
 * the import here is purely for compile-time selector validation, it does NOT
 * pull the chart into the main bundle.
 */
@Component({
  selector: 'tms-instructor-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AnalyticsChartComponent],
  templateUrl: './instructor-dashboard.component.html',
  styleUrl: './instructor-dashboard.component.scss',
})
export class InstructorDashboardComponent implements OnInit {
  store = inject(EnrollmentStore);

  ngOnInit() {
    this.store.loadEnrollments();
  }
}

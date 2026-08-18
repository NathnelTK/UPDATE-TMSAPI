import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Enrollment } from '../../models/enrollment.model';

/**
 * Simulates a heavyweight charting library. In a production TMS this would be a
 * real chart (Chart.js, ngx-charts, …) rendering enrollment trends. A styled
 * placeholder with enough internal logic is sufficient to produce a measurable
 * separate JS chunk when loaded inside a @defer block.
 */
@Component({
  selector: 'tms-analytics-chart',
  standalone: true,
  // OnPush: this component only re-checks when its signal input `data` changes
  // reference or a signal it reads emits. Fits the signal-first pattern M9 teaches.
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chart-container">
      <h3>Enrollment Analytics</h3>
      <div class="chart-bars">
        <div class="bar approved" [style.height.px]="approvedHeight()">
          <span>Approved</span>
        </div>
        <div class="bar pending" [style.height.px]="pendingHeight()">
          <span>Pending</span>
        </div>
        <div class="bar rejected" [style.height.px]="rejectedHeight()">
          <span>Rejected</span>
        </div>
      </div>
      <p class="chart-summary">Total records: {{ data().length }}</p>
    </div>
  `,
  styleUrl: './analytics-chart.component.scss',
})
export class AnalyticsChartComponent {
  data = input.required<Enrollment[]>();

  // computed() memoizes the result — the filter only re-runs when data() changes,
  // not on every change-detection cycle.
  approvedHeight = computed(() => {
    const count = this.data().filter((e) => e.status === 'Approved').length;
    return Math.max(20, count * 3);
  });

  pendingHeight = computed(() => {
    const count = this.data().filter((e) => e.status === 'Pending').length;
    return Math.max(20, count * 3);
  });

  rejectedHeight = computed(() => {
    const count = this.data().filter((e) => e.status === 'Rejected').length;
    return Math.max(20, count * 3);
  });
}

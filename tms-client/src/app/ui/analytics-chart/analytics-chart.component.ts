import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Enrollment } from '../../models/enrollment.model';

/**
 * Lightweight enrollment breakdown chart. In a production TMS this would be a
 * real charting library; a self-contained bar chart with enough internal logic
 * is enough to produce a measurable separate JS chunk when loaded inside the
 * instructor dashboard's @defer block. Bars scale relative to the largest bucket.
 */
@Component({
  selector: 'tms-analytics-chart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chart">
      <div class="chart-head">
        <h3>Enrollment analytics</h3>
        <span class="total tms-muted">{{ total() }} total records</span>
      </div>

      <div class="bars">
        <div class="col">
          <div class="track">
            <div class="bar approved" [style.height.%]="approvedPct()">
              <span class="val">{{ approvedCount() }}</span>
            </div>
          </div>
          <span class="cat">Approved</span>
        </div>
        <div class="col">
          <div class="track">
            <div class="bar pending" [style.height.%]="pendingPct()">
              <span class="val">{{ pendingCount() }}</span>
            </div>
          </div>
          <span class="cat">Pending</span>
        </div>
        <div class="col">
          <div class="track">
            <div class="bar rejected" [style.height.%]="rejectedPct()">
              <span class="val">{{ rejectedCount() }}</span>
            </div>
          </div>
          <span class="cat">Rejected</span>
        </div>
      </div>
    </div>
  `,
  styleUrl: './analytics-chart.component.scss',
})
export class AnalyticsChartComponent {
  data = input.required<Enrollment[]>();

  approvedCount = computed(() => this.data().filter((e) => e.status === 'Approved').length);
  pendingCount = computed(() => this.data().filter((e) => e.status === 'Pending').length);
  rejectedCount = computed(() => this.data().filter((e) => e.status === 'Rejected').length);
  total = computed(() => this.data().length);

  // Scale each bar to the largest bucket so the chart always fills nicely.
  private max = computed(() =>
    Math.max(1, this.approvedCount(), this.pendingCount(), this.rejectedCount()),
  );
  approvedPct = computed(() => (this.approvedCount() / this.max()) * 100);
  pendingPct = computed(() => (this.pendingCount() / this.max()) * 100);
  rejectedPct = computed(() => (this.rejectedCount() / this.max()) * 100);
}

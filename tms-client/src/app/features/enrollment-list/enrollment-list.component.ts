import { Component, inject, OnInit } from '@angular/core';
import { EnrollmentStore } from '../../store/enrollment.store';

@Component({
  selector: 'tms-enrollment-list',
  standalone: true,
  templateUrl: './enrollment-list.component.html',
  styleUrl: './enrollment-list.component.scss',
})
export class EnrollmentListComponent implements OnInit {
  // Both EnrollmentListComponent and DashboardSummaryComponent inject the
  // SAME singleton store instance (providedIn: 'root'). When patchState fires,
  // every component bound to store.entities() or store.pendingCount() re-renders
  // automatically — no manual refresh, no duplicate API calls, no state drift.
  store = inject(EnrollmentStore);

  ngOnInit() {
    this.store.loadEnrollments();
  }

  onApprove(id: string) {
    this.store.approveEnrollment(id);
  }
}

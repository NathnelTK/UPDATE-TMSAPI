import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { EnrollmentStore } from '../../store/enrollment.store';
import { Enrollment } from '../../models/enrollment.model';

/**
 * M9 Session 2 · Exercise 3 — Enterprise data grid.
 *
 * Replaces the Session 1 @for card list with an Angular Material MatTable that
 * provides built-in sorting, pagination and accessibility, wired to the same
 * singleton EnrollmentStore (approving here still updates the dashboard summary).
 */
@Component({
  selector: 'tms-enrollment-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule, MatPaginatorModule, MatSortModule],
  templateUrl: './enrollment-list.component.html',
  styleUrl: './enrollment-list.component.scss',
})
export class EnrollmentListComponent {
  store = inject(EnrollmentStore);

  displayedColumns = ['studentName', 'courseName', 'status', 'actions'];

  // MatTableDataSource bridges our store data into Material's rendering pipeline
  // (sorting, pagination, filtering).
  dataSource = new MatTableDataSource<Enrollment>();

  // viewChild.required() is the signal-based replacement for @ViewChild — these
  // are signals that update reactively when Angular resolves the template
  // queries, so no ngAfterViewInit lifecycle hook is needed.
  readonly paginator = viewChild.required(MatPaginator);
  readonly sort = viewChild.required(MatSort);

  constructor() {
    // Push store entities into the data source whenever they change (load,
    // approve, rollback). Reading the entities() signal registers the dependency.
    effect(() => {
      this.dataSource.data = this.store.entities();
    });

    // Wire paginator + sort once Angular resolves the view queries. Because
    // viewChild returns a signal, this effect re-runs when they become available.
    effect(() => {
      this.dataSource.paginator = this.paginator();
      this.dataSource.sort = this.sort();
    });

    // The store is a singleton, so it can load immediately — no lifecycle hook.
    this.store.loadEnrollments();
  }
}

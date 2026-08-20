import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { StudentService } from '../../services/student.service';
import { EnrollmentService } from '../../services/enrollment.service';

/**
 * Student schedule viewer. Pick a student and see the courses they are enrolled in
 * via `GET /api/v2/enrollments/{studentId}/schedule`. The resource stays idle until
 * a student is chosen (request() returns undefined → loader not called).
 */
@Component({
  selector: 'app-schedule',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './schedule.component.html',
  styleUrl: './schedule.component.scss',
})
export class ScheduleComponent {
  private studentSvc = inject(StudentService);
  private enrollmentSvc = inject(EnrollmentService);

  studentsResource = rxResource({ loader: () => this.studentSvc.getAll() });
  selectedStudentId = signal<number | undefined>(undefined);

  scheduleResource = rxResource({
    request: () => this.selectedStudentId(),
    // Loader only runs when request() is a defined number, so the assertion is safe.
    loader: ({ request }) => this.enrollmentSvc.getSchedule(request!),
  });

  select(value: string): void {
    this.selectedStudentId.set(value ? +value : undefined);
  }
}

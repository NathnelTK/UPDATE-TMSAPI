import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { rxResource } from '@angular/core/rxjs-interop';
import { StudentService } from '../../services/student.service';
import { CourseService } from '../../services/course.service';
import { EnrollmentService } from '../../services/enrollment.service';

/**
 * Enrollment form — real `POST /api/v2/enrollments`. Student and course pickers are
 * populated from the live API; the course can be preselected via a `?courseCode=`
 * query param (deep-link from the course-detail page). Success and error states come
 * straight from the API (RFC-7807 `detail` on failure).
 */
@Component({
  selector: 'app-enrollment-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './enrollment-form.component.html',
  styleUrl: './enrollment-form.component.scss',
})
export class EnrollmentFormComponent {
  private fb = inject(FormBuilder);
  private studentSvc = inject(StudentService);
  private courseSvc = inject(CourseService);
  private enrollmentSvc = inject(EnrollmentService);

  // Bound from the ?courseCode= query param via withComponentInputBinding().
  courseCode = input<string>('');

  studentsResource = rxResource({ loader: () => this.studentSvc.getAll() });
  coursesResource = rxResource({ loader: () => this.courseSvc.getAll() });

  loading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  form = this.fb.group({
    studentId: this.fb.control<number | null>(null, [Validators.required]),
    courseCode: this.fb.control('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Z]{2,4}-\d{3}$/)],
    }),
  });

  constructor() {
    // Preselect the course when arriving from a course-detail deep-link.
    effect(() => {
      const code = this.courseCode();
      if (code) {
        this.form.controls.courseCode.setValue(code);
      }
    });
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    const { studentId, courseCode } = this.form.getRawValue();
    try {
      await firstValueFrom(this.enrollmentSvc.enroll({ studentId: studentId!, courseCode }));
      this.success.set(`Enrollment submitted for ${courseCode}. It is now pending approval.`);
    } catch (err: unknown) {
      const detail = (err as { error?: { detail?: string } })?.error?.detail;
      this.error.set(detail ?? 'Enrollment failed. Please review the details and try again.');
    } finally {
      this.loading.set(false);
    }
  }

  enrollAnother(): void {
    this.success.set(null);
    this.form.controls.studentId.reset(null);
  }
}

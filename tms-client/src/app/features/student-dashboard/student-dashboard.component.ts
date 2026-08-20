import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { CourseCardComponent } from '../../ui/course-card/course-card.component';
import { Course } from '../../models/course.model';
import { CourseService } from '../../services/course.service';
import { AuthService } from '../../services/auth.service';

/**
 * Dashboard / course catalog. Loads the live V2 course list, shows a few
 * derived KPIs, and lets the user jump straight into the enrollment form for a
 * chosen course (deep-linked by course code).
 */
@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CourseCardComponent],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.scss',
})
export class StudentDashboardComponent {
  private api = inject(CourseService);
  private auth = inject(AuthService);
  private router = inject(Router);

  user = this.auth.currentUser;

  // rxResource wraps the Observable into managed isLoading/error/value signals,
  // subscribing on creation and cleaning up on destroy — no manual subscription.
  coursesResource = rxResource({
    loader: () => this.api.getAll(),
  });

  private courses = computed<Course[]>(() => this.coursesResource.value() ?? []);

  totalCourses = computed(() => this.courses().length);
  openCourses = computed(
    () => this.courses().filter((c) => c.enrollmentCount < c.maxCapacity).length,
  );
  openSeats = computed(() =>
    this.courses().reduce((sum, c) => sum + Math.max(0, c.maxCapacity - c.enrollmentCount), 0),
  );

  handleEnroll(course: Course) {
    this.router.navigate(['/enroll'], { queryParams: { courseCode: course.code } });
  }
}

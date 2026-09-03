import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Course } from '../../models/course.model';
import { CourseService } from '../../services/course.service';
import { AuthService } from '../../services/auth.service';
import { LearningService } from '../../services/learning.service';
import { of, Observable, switchMap } from 'rxjs';
import { LearningSummary } from '../../models/learning.model';

/**
 * Dashboard / course catalog. Loads the live V2 course list, shows a few
 * derived KPIs, and lets the user jump straight into the enrollment form for a
 * chosen course (deep-linked by course code).
 */
@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.scss',
})
export class StudentDashboardComponent {
  private api = inject(CourseService);
  protected auth = inject(AuthService);
  private router = inject(Router);
  private learning = inject(LearningService);

  user = this.auth.currentUser;

  // rxResource wraps the Observable into managed isLoading/error/value signals,
  // subscribing on creation and cleaning up on destroy — no manual subscription.
  coursesResource = rxResource({
    loader: () => this.api.getAll(),
  });

  studentResource = rxResource({ loader: () => this.learning.getCurrentStudent() });
  summaryResource = rxResource({
    loader: (): Observable<LearningSummary> => this.learning.getCurrentStudent().pipe(
      switchMap((student) => this.learning.getSummary(student.id)),
    ),
  });
  adminResource = rxResource({ loader: () => this.learning.getAdminDashboard() });

  private courses = computed<Course[]>(() => this.coursesResource.value() ?? []);

  searchTerm = signal('');
  selectedTrack = signal('All tracks');

  readonly trackFor = (course: Course): string => {
    if (course.code.startsWith('CSE')) return 'Engineering';
    if (course.code.startsWith('DAT')) return 'Data';
    if (course.code.startsWith('ARC') || course.code.startsWith('DEV')) return 'Architecture';
    if (course.code.startsWith('AI')) return 'AI & Emerging Tech';
    return 'Product & Design';
  };

  tracks = computed(() => ['All tracks', ...new Set(this.courses().map(this.trackFor))]);
  filteredCourses = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    const track = this.selectedTrack();
    return this.courses().filter((course) => {
      const matchesQuery = !query || `${course.code} ${course.title}`.toLowerCase().includes(query);
      return matchesQuery && (track === 'All tracks' || this.trackFor(course) === track);
    });
  });

  totalCourses = computed(() => this.courses().length);
  openCourses = computed(
    () => this.courses().filter((c) => c.enrollmentCount < c.maxCapacity).length,
  );
  openSeats = computed(() =>
    this.courses().reduce((sum, c) => sum + Math.max(0, c.maxCapacity - c.enrollmentCount), 0),
  );

  completionRate = computed(() => this.summaryResource.value()?.attendancePercentage ?? 0);

  setTrack(track: string): void {
    this.selectedTrack.set(track);
  }

  handleEnroll(course: Course) {
    this.router.navigate(['/enroll'], { queryParams: { courseCode: course.code } });
  }
}

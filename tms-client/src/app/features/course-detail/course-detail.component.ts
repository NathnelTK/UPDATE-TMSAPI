import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { CourseService } from '../../services/course.service';

/**
 * Course detail page. Fetches a single course via `CourseService.getById`
 * (which unwraps the V2 `{ data, links }` envelope) and renders capacity,
 * seats remaining and a working Enroll action that deep-links into the
 * enrollment form with the course code preselected.
 */
@Component({
  selector: 'app-course-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './course-detail.component.html',
  styleUrl: './course-detail.component.scss',
})
export class CourseDetailComponent {
  private api = inject(CourseService);

  // withComponentInputBinding() maps the :id route param (a string) onto this input.
  id = input.required<string>();
  private courseId = computed(() => Number(this.id()));

  courseResource = rxResource({
    request: () => this.courseId(),
    loader: ({ request }) => this.api.getById(request),
  });

  private course = this.courseResource.value;

  seatsLeft = computed(() => {
    const c = this.course();
    return c ? Math.max(0, c.maxCapacity - c.enrollmentCount) : 0;
  });

  fillPct = computed(() => {
    const c = this.course();
    if (!c || !c.maxCapacity) return 0;
    return Math.min(100, Math.round((c.enrollmentCount / c.maxCapacity) * 100));
  });

  isFull = computed(() => {
    const c = this.course();
    return !!c && c.enrollmentCount >= c.maxCapacity;
  });
}

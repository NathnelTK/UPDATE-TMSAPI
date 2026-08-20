import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Course } from '../../models/course.model';

@Component({
  selector: 'tms-course-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './course-card.component.html',
  styleUrl: './course-card.component.scss',
})
export class CourseCardComponent {
  // input.required<T>() — parent MUST pass a Course; compile-time error if omitted.
  course = input.required<Course>();

  // output<T>() — emits the Course up to the parent when Enroll is clicked.
  enrollClicked = output<Course>();

  isFull = computed(() => this.course().enrollmentCount >= this.course().maxCapacity);
  fillPct = computed(() => {
    const c = this.course();
    if (!c.maxCapacity) return 0;
    return Math.min(100, Math.round((c.enrollmentCount / c.maxCapacity) * 100));
  });
}

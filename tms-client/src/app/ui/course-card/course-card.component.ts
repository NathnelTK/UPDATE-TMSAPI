import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Course } from '../../models/course.model';

@Component({
  selector: 'tms-course-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './course-card.component.html',
  styleUrl: './course-card.component.scss',
})
export class CourseCardComponent {
  // input.required<T>() — parent MUST pass a Course; compile-time error if omitted.
  course = input.required<Course>();

  // output<T>() — emits a Course event up to the parent when Enroll is clicked.
  enrollClicked = output<Course>();
}

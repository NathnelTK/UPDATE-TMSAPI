import { Component, input, effect } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './course-detail.component.html',
})
export class CourseDetailComponent {
  // withComponentInputBinding() in app.config.ts maps the URL param :id
  // directly to this input — name must match the route param exactly.
  id = input.required<string>();

  constructor() {
    // effect() re-runs every time id() changes (e.g. /courses/1 → /courses/2).
    effect(() => {
      console.log(`Loading course detail for ID: ${this.id()}`);
    });
  }
}

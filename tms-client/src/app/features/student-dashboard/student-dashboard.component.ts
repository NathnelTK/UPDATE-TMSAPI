import { Component, signal, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { CourseCardComponent } from '../../ui/course-card/course-card.component';
import { Course } from '../../models/course.model';
import { CourseService } from '../../services/course.service';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CourseCardComponent],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.scss',
})
export class StudentDashboardComponent {
  private api = inject(CourseService);

  studentName = signal('Liya Kebede');
  earnedCredits = signal(45);

  graduationStatus = computed(() =>
    this.earnedCredits() >= 120 ? 'Eligible for Graduation' : 'In Progress',
  );

  registerForClass() {
    this.earnedCredits.update((c) => c + 3);
  }

  // rxResource wraps the Observable from CourseService into three managed signals:
  //   .isLoading() → true while the HTTP request is in-flight
  //   .error()     → error object if the request fails
  //   .value()     → Course[] when the request succeeds
  // It subscribes on creation and unsubscribes when the component is destroyed —
  // no manual .subscribe() / .unsubscribe() needed.
  coursesResource = rxResource({
    loader: () => this.api.getAll(),
  });

  selectedCourse = signal<Course | null>(null);

  handleEnroll(course: Course) {
    this.selectedCourse.set(course);
    console.log('Enrollment requested for:', course.title);
  }
}

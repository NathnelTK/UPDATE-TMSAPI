import { Component, signal, computed } from '@angular/core';
import { CourseCardComponent } from '../../ui/course-card/course-card.component';
import { Course } from '../../models/course.model';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  // CourseCardComponent must be listed here so Angular recognises <tms-course-card>
  imports: [CourseCardComponent],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.scss',
})
export class StudentDashboardComponent {
  studentName = signal('Liya Kebede');
  earnedCredits = signal(45);

  graduationStatus = computed(() =>
    this.earnedCredits() >= 120 ? 'Eligible for Graduation' : 'In Progress',
  );

  registerForClass() {
    this.earnedCredits.update((c) => c + 3);
  }

  // Ex 3: catalog as a signal so the empty-state template reacts to changes
  availableCourses = signal<Course[]>([
    { id: 1, title: 'Advanced Java Services',    code: 'CSE-101', maxCapacity: 30, enrollmentCount: 10 },
    { id: 2, title: 'Angular UI Lab',            code: 'CSE-210', maxCapacity: 25, enrollmentCount: 25 },
    { id: 3, title: 'Database Design',           code: 'CSE-305', maxCapacity: 20, enrollmentCount: 18 },
    { id: 4, title: 'API Security Workshop',     code: 'CSE-420', maxCapacity: 40, enrollmentCount: 15 },
  ]);

  // Tracks which course was last selected so the template can confirm it
  selectedCourse = signal<Course | null>(null);

  handleEnroll(course: Course) {
    this.selectedCourse.set(course);
    console.log('Enrollment requested for:', course.title);
  }
}

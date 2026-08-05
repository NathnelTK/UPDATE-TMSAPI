import { Component, signal, computed } from '@angular/core';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.scss',
})
export class StudentDashboardComponent {
  // signal() creates a reactive variable — Angular watches it and
  // automatically re-renders any template that reads it when it changes.
  studentName = signal('Liya Kebede');
  earnedCredits = signal(45);

  // computed() creates a read-only derived signal.
  // It recalculates automatically whenever earnedCredits() changes.
  graduationStatus = computed(() =>
    this.earnedCredits() >= 120 ? 'Eligible for Graduation' : 'In Progress',
  );

  // .update() receives the current value and returns the new value.
  registerForClass() {
    this.earnedCredits.update((c) => c + 3);
  }
}

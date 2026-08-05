import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  Validators,
  ReactiveFormsModule,
  FormArray,
} from '@angular/forms';

@Component({
  selector: 'app-enrollment-form',
  standalone: true,
  // ReactiveFormsModule is required — without it Angular does not recognise
  // formGroup, formControlName, or formControl directives.
  imports: [ReactiveFormsModule],
  templateUrl: './enrollment-form.component.html',
  styleUrl: './enrollment-form.component.scss',
})
export class EnrollmentFormComponent {
  // inject() is Angular's function-based DI — equivalent to constructor injection in .NET.
  private fb = inject(FormBuilder);

  // Signal tracks whether the form was submitted (drives @if in template).
  submitted = signal(false);

  // nonNullable.group() ensures field values are typed as 'string' not 'string | null',
  // saving null-checks everywhere. Each field is [defaultValue, validators].
  form = this.fb.nonNullable.group({
    studentId: [
      '',
      [Validators.required, Validators.pattern('^STU-[0-9]{4}$')],
    ],
    courseId: ['', Validators.required],
    term: ['Fall 2026', Validators.required], // Pre-filled default
    notes: [''],                              // Optional — no validators
    backupCourses: this.fb.array<FormControl<string>>([]),
  });

  // Property accessor — shorthand for this.form.controls.backupCourses.
  get backups() {
    return this.form.controls.backupCourses;
  }

  addBackup() {
    this.backups.push(
      this.fb.control('', { nonNullable: true, validators: Validators.required }),
    );
  }

  removeBackup(index: number) {
    this.backups.removeAt(index);
  }

  submit() {
    if (this.form.valid) {
      // getRawValue() always includes disabled fields — never use .value alone.
      const payload = this.form.getRawValue();
      console.log('Enrollment payload:', payload);
      this.submitted.set(true);
    } else {
      // markAllAsTouched() forces Angular to show validation errors on every field,
      // not only the ones the user has interacted with.
      this.form.markAllAsTouched();
    }
  }
}

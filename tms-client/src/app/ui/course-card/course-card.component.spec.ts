import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CourseCardComponent } from './course-card.component';
import { Course } from '../../models/course.model';

/**
 * M12 Session 2 — Component testing with signal inputs and outputs.
 *
 * `input.required<Course>()` is set via `componentRef.setInput()` (the supported
 * way to feed required signal inputs in a test), then a change-detection pass
 * evaluates the `isFull`/`fillPct` computeds and renders the template. We assert
 * both the computed values and the resulting DOM, plus that clicking Enroll emits
 * the course through the `output()`.
 */
describe('CourseCardComponent', () => {
  let fixture: ComponentFixture<CourseCardComponent>;

  const open: Course = {
    id: 1,
    code: 'CSE-101',
    title: 'Intro to CS',
    maxCapacity: 30,
    enrollmentCount: 15,
  };
  const full: Course = { ...open, enrollmentCount: 30 };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseCardComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseCardComponent);
  });

  function setCourse(course: Course): void {
    fixture.componentRef.setInput('course', course);
    fixture.detectChanges();
  }

  it('renders an open course: 50% fill, "Open" badge, enabled Enroll button', () => {
    setCourse(open);
    const el: HTMLElement = fixture.nativeElement;

    expect(fixture.componentInstance.isFull()).toBeFalse();
    expect(fixture.componentInstance.fillPct()).toBe(50);
    expect(el.querySelector('.tms-badge')?.textContent?.trim()).toBe('Open');
    expect(el.querySelector('button')?.disabled).toBeFalse();
  });

  it('renders a full course: 100% fill, "Full" badge, disabled Enroll button', () => {
    setCourse(full);
    const el: HTMLElement = fixture.nativeElement;

    expect(fixture.componentInstance.isFull()).toBeTrue();
    expect(fixture.componentInstance.fillPct()).toBe(100);
    expect(el.querySelector('.tms-badge')?.textContent?.trim()).toBe('Full');
    expect(el.querySelector('button')?.disabled).toBeTrue();
  });

  it('emits enrollClicked with the course when Enroll is pressed', () => {
    setCourse(open);
    let emitted: Course | undefined;
    fixture.componentInstance.enrollClicked.subscribe((c) => (emitted = c));

    fixture.nativeElement.querySelector('button')?.click();

    expect(emitted).toEqual(open);
  });
});

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { rxResource } from '@angular/core/rxjs-interop';
import { CourseService } from '../../services/course.service';
import { CourseCardComponent } from '../../ui/course-card/course-card.component';

@Component({ selector: 'app-course-catalog', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush, imports: [FormsModule, CourseCardComponent], template: `
<section class="page-stack"><div class="page-heading"><span class="eyebrow">TRAINING CATALOG</span><h2>Find your next course</h2><p>Browse current training offerings and review capacity before submitting an enrollment request.</p></div>
<div class="catalog-toolbar tms-card"><label class="search-box"><span class="material-icons">search</span><input [ngModel]="search()" (ngModelChange)="search.set($event)" placeholder="Search by course title or code" aria-label="Search courses"></label><span class="result-count">{{ filtered().length }} courses</span></div>
@if (courses.isLoading()) { <div class="catalog-grid">@for (i of [1,2,3,4,5,6]; track i) { <div class="tms-card loading-card"></div> }</div> } @else if (courses.error()) { <div class="tms-card state error">Courses are temporarily unavailable. Refresh after confirming the API is running.</div> } @else { <div class="catalog-grid">@for (course of filtered(); track course.id) { <tms-course-card [course]="course" (enrollClicked)="open($event)" /> } @empty { <div class="tms-card state">No courses match your search.</div> }</div>}</section>` })
export class CourseCatalogComponent {
  private readonly service = inject(CourseService);
  readonly courses = rxResource({ loader: () => this.service.getAll() });
  readonly search = signal('');
  readonly filtered = computed(() => { const query = this.search().trim().toLowerCase(); return (this.courses.value() ?? []).filter((course) => !query || `${course.code} ${course.title}`.toLowerCase().includes(query)); });
  open(course: { id: number }): void { window.location.href = `/courses/${course.id}`; }
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LearningService } from '../../services/learning.service';
import { Observable, switchMap } from 'rxjs';
import { AttendanceRecord } from '../../models/learning.model';

@Component({ selector: 'app-learning-records', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush, template: `
<section class="page-stack"><div class="page-heading"><span class="eyebrow">MY LEARNING</span><h2>{{ title }}</h2><p>{{ description }}</p></div>
@if (studentResource.isLoading()) { <div class="tms-card state">Loading student profile...</div> }
@else if (studentResource.error()) { <div class="tms-card state error">Unable to load your student profile.</div> }
@else if (records.isLoading()) { <div class="tms-card state">Loading records...</div> }
@else if (records.error()) { <div class="tms-card state error">Unable to load records. Please try again later.</div> }
@else { <div class="record-list">@for (row of records.value() ?? []; track row.id) { <article class="tms-card record-row"><div><strong>{{ row.primary }}</strong><span>{{ row.secondary }}</span></div><b>{{ row.value }}</b></article> } @empty { <div class="tms-card state">No records have been published yet.</div> }</div> }</section>` })
export class LearningRecordsComponent {
  private readonly service = inject(LearningService);
  readonly studentResource = rxResource({ loader: () => this.service.getCurrentStudent() });
  readonly title = 'Learning records';
  readonly description = 'Your published training activity and results.';
  readonly records = rxResource({ loader: (): Observable<AttendanceRecord[]> => this.service.getCurrentStudent().pipe(switchMap((student) => this.service.getAttendance(student.id))) });
}

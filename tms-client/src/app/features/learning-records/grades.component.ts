import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LearningService } from '../../services/learning.service';
import { Observable, switchMap } from 'rxjs';
import { GradeRecord } from '../../models/learning.model';

@Component({ selector: 'app-grades', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush, template: `<section class="page-stack"><div class="page-heading"><span class="eyebrow">ASSESSMENTS</span><h2>My grades</h2><p>Scores published by your instructors.</p></div>@if (studentResource.isLoading() || grades.isLoading()) { <div class="tms-card state">Loading grades...</div> } @else if (grades.error()) { <div class="tms-card state error">Unable to load grades.</div> } @else { <div class="record-list">@for (row of grades.value() ?? []; track row.id) { <article class="tms-card record-row"><div><strong>{{ row.assessmentTitle }}</strong><span>{{ row.score }} / {{ row.maximumScore }} · Weight {{ row.weight }}</span></div><b>{{ ((row.score / row.maximumScore) * 100).toFixed(0) }}%</b></article>} @empty { <div class="tms-card state">No grades have been published yet.</div>}</div>}</section>` })
export class GradesComponent { private readonly service = inject(LearningService); readonly studentResource = rxResource({ loader: () => this.service.getCurrentStudent() }); readonly grades = rxResource({ loader: (): Observable<GradeRecord[]> => this.service.getCurrentStudent().pipe(switchMap((student) => this.service.getGrades(student.id))) }); }

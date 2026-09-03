import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LearningService } from '../../services/learning.service';
import { DatePipe } from '@angular/common';

@Component({ selector: 'app-grants', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush, template: `<section class="page-stack"><div class="page-heading"><span class="eyebrow">FUNDING SUPPORT</span><h2>Grant applications</h2><p>Track government and partner training support.</p></div>@if (applications.isLoading()) { <div class="tms-card state">Loading applications...</div> } @else if (applications.error()) { <div class="tms-card state error">Unable to load grant applications.</div> } @else { <div class="record-list">@for (row of applications.value() ?? []; track row.id) { <article class="tms-card record-row"><div><strong>{{ row.grantProgramName }}</strong><span>Eligibility score {{ row.eligibilityScore }} | {{ row.applicationDate | date:'mediumDate' }}</span></div><b>{{ row.status }}</b></article>} @empty { <div class="tms-card state">You have no grant applications.</div>}</div>}</section>`, imports: [DatePipe] })
export class GrantsComponent { private readonly service = inject(LearningService); readonly applications = rxResource({ loader: () => this.service.getMyGrantApplications() }); }

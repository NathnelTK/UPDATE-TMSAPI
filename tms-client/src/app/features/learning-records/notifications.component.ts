import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LearningService } from '../../services/learning.service';
import { DatePipe } from '@angular/common';

@Component({ selector: 'app-notifications', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush, template: `<section class="page-stack"><div class="page-heading"><span class="eyebrow">UPDATES</span><h2>Notifications</h2><p>Important changes to your training journey.</p></div>@if (items.isLoading()) { <div class="tms-card state">Loading notifications...</div> } @else if (items.error()) { <div class="tms-card state error">Unable to load notifications.</div> } @else { <div class="record-list">@for (row of items.value() ?? []; track row.id) { <article class="tms-card record-row"><div><strong>{{ row.title }}</strong><span>{{ row.message }}</span></div><small>{{ row.createdAt | date:'medium' }}</small></article>} @empty { <div class="tms-card state">You have no notifications.</div>}</div>}</section>`, imports: [DatePipe] })
export class NotificationsComponent { private readonly service = inject(LearningService); readonly items = rxResource({ loader: () => this.service.getNotifications() }); }

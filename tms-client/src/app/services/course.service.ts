import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { Course, CourseDetail, CourseLink, PagedResponse } from '../models/course.model';

// @Injectable({ providedIn: 'root' }) — Angular creates one singleton instance
// shared across the entire app, similar to AddSingleton<T>() in .NET DI.
@Injectable({ providedIn: 'root' })
export class CourseService {
  private http = inject(HttpClient);

  // M7 produced two envelopes:
  //   GET /api/v1/courses → { items: Course[], totalCount, … }  (map p.items)
  //   GET /api/v2/courses → { data: Course[], meta: {…}, links: {…} } (map p.data)
  // This service targets the V2 endpoint. The base URL is no longer hardcoded —
  // it comes from environment.apiUrl (M10 S1 Ex1) so dev hits the cross-origin
  // backend and production can use a same-origin relative path.
  private baseUrl = `${environment.apiUrl}/courses`;

  getAll() {
    // V2 envelope: data[] carries the rows, meta carries paging.
    // Cast to any to handle the V2 shape without duplicating the interface.
    return this.http
      .get<any>(this.baseUrl, { params: { page: '1', pageSize: '50' } })
      .pipe(map((p) => (p.data ?? p.items) as Course[]));
  }

  // V2 detail envelope: { data: Course, links: LinkDto[] }. Flatten it into the
  // CourseDetail the UI expects (course fields + a `links` array). The old code
  // typed the raw response as CourseDetail, so `links` was always undefined and
  // the course fields sat under `.data` — this unwraps both correctly.
  getById(id: number) {
    return this.http
      .get<{ data: Course; links: CourseLink[] }>(`${this.baseUrl}/${id}`)
      .pipe(map((res) => ({ ...res.data, links: res.links ?? [] }) as CourseDetail));
  }

  // M10 Session 3 - Exercise 3: delete a course. Returns the raw Observable so
  // CourseStore can subscribe and roll back optimistically if the server replies
  // 409 Conflict (active enrollments) with an RFC 7807 ProblemDetails body.
  delete(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

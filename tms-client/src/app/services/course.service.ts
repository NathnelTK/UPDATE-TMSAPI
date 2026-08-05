import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { Course, CourseDetail, PagedResponse } from '../models/course.model';

// @Injectable({ providedIn: 'root' }) — Angular creates one singleton instance
// shared across the entire app, similar to AddSingleton<T>() in .NET DI.
@Injectable({ providedIn: 'root' })
export class CourseService {
  private http = inject(HttpClient);

  // M7 produced two envelopes:
  //   GET /api/v1/courses → { items: Course[], totalCount, … }  (map p.items)
  //   GET /api/v2/courses → { data: Course[], meta: {…}, links: {…} } (map p.data)
  // This service targets the V2 endpoint; swap baseUrl and map key if using V1.
  private baseUrl = 'https://localhost:7190/api/v2/courses';

  getAll() {
    // V2 envelope: data[] carries the rows, meta carries paging.
    // Cast to any to handle the V2 shape without duplicating the interface.
    return this.http
      .get<any>(this.baseUrl, { params: { page: '1', pageSize: '50' } })
      .pipe(map((p) => (p.data ?? p.items) as Course[]));
  }

  getById(id: string) {
    return this.http.get<CourseDetail>(`${this.baseUrl}/${id}`);
  }
}

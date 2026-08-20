import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Enrollment, EnrollRequest, Schedule } from '../models/enrollment.model';

/**
 * Talks to the V2 enrollments API. Reads (`getAll`, `getSchedule`) are anonymous;
 * `approve`/`reject` hit `[Authorize]` endpoints, so the auth interceptor's bearer
 * token is genuinely exercised end-to-end.
 */
@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/enrollments`;

  /** GET /api/v2/enrollments — the approval queue (Pending first). */
  getAll(): Observable<Enrollment[]> {
    return this.http.get<Enrollment[]>(this.baseUrl);
  }

  /** POST /api/v2/enrollments/{id}/approve — registrar approves (auth required). */
  approve(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/approve`, {});
  }

  /** POST /api/v2/enrollments/{id}/reject — registrar rejects (auth required). */
  reject(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/reject`, {});
  }

  /** POST /api/v2/enrollments — enroll a student by course code. */
  enroll(request: EnrollRequest): Observable<unknown> {
    return this.http.post(this.baseUrl, request);
  }

  /** GET /api/v2/enrollments/{studentId}/schedule — a student's enrolled courses. */
  getSchedule(studentId: number): Observable<Schedule> {
    return this.http.get<Schedule>(`${this.baseUrl}/${studentId}/schedule`);
  }
}

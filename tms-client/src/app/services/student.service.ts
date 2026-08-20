import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Student } from '../models/student.model';

/** Read-only student directory — feeds the enrollment form's student picker. */
@Injectable({ providedIn: 'root' })
export class StudentService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/students`;

  /** GET /api/v2/students — active students, ordered by name. */
  getAll(): Observable<Student[]> {
    return this.http.get<Student[]>(this.baseUrl);
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AttendanceRecord, GradeRecord, GrantApplication, GrantProgram, LearningSummary, NotificationRecord } from '../models/learning.model';
import { CurrentStudent } from '../models/student.model';

@Injectable({ providedIn: 'root' })
export class LearningService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;
  getCurrentStudent() { return this.http.get<CurrentStudent>(`${this.api}/students/me`); }
  getSummary(id: number) { return this.http.get<LearningSummary>(`${this.api}/students/${id}/summary`); }
  getAttendance(id: number) { return this.http.get<AttendanceRecord[]>(`${this.api}/attendance`, { params: { studentId: id } }); }
  getGrades(id: number) { return this.http.get<GradeRecord[]>(`${this.api}/grades`, { params: { studentId: id } }); }
  getNotifications() { return this.http.get<NotificationRecord[]>(`${this.api}/notifications/my`); }
  getGrantPrograms() { return this.http.get<GrantProgram[]>(`${this.api}/grants`); }
  getMyGrantApplications() { return this.http.get<GrantApplication[]>(`${this.api}/grants/applications/my`); }
  getMySchedule(id: number) { return this.http.get<{ studentId: number; courses: { courseCode: string; title: string; schedule: string }[] }>(`${this.api}/enrollments/${id}/schedule`); }
  getAdminDashboard() { return this.http.get<{ totalStudents: number; activeStudents: number; totalCourses: number; pendingEnrollments: number; activeEnrollments: number; completedEnrollments: number; pendingGrantApplications: number; allocatedGrantAmount: number; remainingGrantBudget: number }>(`${this.api}/admin/dashboard`); }
}

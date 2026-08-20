/** Enrollment approval workflow states — mirrors the .NET `EnrollmentStatus` enum. */
export type EnrollmentStatus = 'Pending' | 'Approved' | 'Rejected';

/**
 * Approval-queue row — mirrors `EnrollmentListItemDto` from `GET /api/v2/enrollments`.
 * `id` is the numeric PK (used as the SignalStore entity key and in approve/reject URLs).
 */
export interface Enrollment {
  id: number;
  studentId: number;
  studentName: string;
  courseId: number;
  courseCode: string;
  courseName: string;
  status: EnrollmentStatus;
  enrolledAt: string;
}

/** Body for `POST /api/v2/enrollments` — mirrors `EnrollStudentCommand`. */
export interface EnrollRequest {
  studentId: number;
  /** Must match XXX-000 (e.g. CSE-101) — enforced by the API's FluentValidation rule. */
  courseCode: string;
}

/** One row of a student's schedule — mirrors `ScheduleItemDto`. */
export interface ScheduleItem {
  courseCode: string;
  title: string;
  schedule: string;
}

/** `GET /api/v2/enrollments/{studentId}/schedule` — mirrors `ScheduleDto`. */
export interface Schedule {
  studentId: number;
  courses: ScheduleItem[];
}

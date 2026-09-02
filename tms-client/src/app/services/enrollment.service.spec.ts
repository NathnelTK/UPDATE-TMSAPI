import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { EnrollmentService } from './enrollment.service';
import { Enrollment, EnrollRequest } from '../models/enrollment.model';
import { environment } from '../../environments/environment';

/**
 * M12 Session 2 — Unit-testing an HttpClient service with HttpTestingController.
 *
 * No real network happens: provideHttpClientTesting() swaps in a mock backend, so
 * each test asserts the exact URL, verb and body the service produces, then flushes
 * a canned response. `httpMock.verify()` fails the test if any unexpected request
 * was made or an expected one was never flushed.
 */
describe('EnrollmentService', () => {
  const base = `${environment.apiUrl}/enrollments`;
  let service: EnrollmentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        EnrollmentService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(EnrollmentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues GET .../enrollments and returns the queue', () => {
    const rows: Enrollment[] = [
      {
        id: 1,
        studentId: 10,
        studentName: 'Ada Lovelace',
        courseId: 1,
        courseCode: 'CSE-101',
        courseName: 'Intro to CS',
        status: 'Pending',
        enrolledAt: '2026-01-01T00:00:00Z',
      },
    ];

    let received: Enrollment[] | undefined;
    service.getAll().subscribe((r) => (received = r));

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush(rows);

    expect(received).toEqual(rows);
  });

  it('approve(id) POSTs to .../{id}/approve with an empty body', () => {
    service.approve(7).subscribe();

    const req = httpMock.expectOne(`${base}/7/approve`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(null);
  });

  it('reject(id) POSTs to .../{id}/reject with an empty body', () => {
    service.reject(7).subscribe();

    const req = httpMock.expectOne(`${base}/7/reject`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush(null);
  });

  it('enroll(request) POSTs the command body to .../enrollments', () => {
    const body: EnrollRequest = { studentId: 10, courseCode: 'CSE-101' };
    service.enroll(body).subscribe();

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });
});

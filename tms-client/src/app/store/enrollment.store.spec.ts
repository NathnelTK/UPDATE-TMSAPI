import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { EnrollmentStore } from './enrollment.store';
import { EnrollmentService } from '../services/enrollment.service';
import { Enrollment } from '../models/enrollment.model';

/**
 * M12 Session 2 — Unit-testing the @ngrx/signals SignalStore in isolation.
 *
 * The store injects EnrollmentService, so we swap it for a Jasmine spy and drive
 * the store through its public rxMethods. Because the spy returns synchronous
 * `of(...)` observables, state settles immediately and can be asserted without
 * fakeAsync/tick. The optimistic-update tests prove the UX contract: the entity
 * flips instantly, and a server failure rolls it back and surfaces an error.
 */
describe('EnrollmentStore', () => {
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
    {
      id: 2,
      studentId: 11,
      studentName: 'Alan Turing',
      courseId: 2,
      courseCode: 'CSE-201',
      courseName: 'Algorithms',
      status: 'Approved',
      enrolledAt: '2026-01-02T00:00:00Z',
    },
  ];

  let api: jasmine.SpyObj<EnrollmentService>;
  let store: InstanceType<typeof EnrollmentStore>;

  function configure(): void {
    TestBed.configureTestingModule({
      providers: [{ provide: EnrollmentService, useValue: api }],
    });
    store = TestBed.inject(EnrollmentStore);
  }

  function statusOf(id: number) {
    return store.entities().find((e) => e.id === id)?.status;
  }

  beforeEach(() => {
    api = jasmine.createSpyObj<EnrollmentService>('EnrollmentService', [
      'getAll',
      'approve',
      'reject',
    ]);
    api.getAll.and.returnValue(of(rows));
    api.approve.and.returnValue(of(void 0));
    api.reject.and.returnValue(of(void 0));
  });

  it('loadEnrollments() fills the collection and derives the counts', () => {
    configure();
    store.loadEnrollments();

    expect(store.total()).toBe(2);
    expect(store.pendingCount()).toBe(1);
    expect(store.approvedCount()).toBe(1);
    expect(store.isLoading()).toBeFalse();
  });

  it('approveEnrollment() optimistically flips status and calls the API', () => {
    configure();
    store.loadEnrollments();

    store.approveEnrollment(1);

    expect(statusOf(1)).toBe('Approved');
    expect(api.approve).toHaveBeenCalledOnceWith(1);
    expect(store.approvedCount()).toBe(2);
  });

  it('approveEnrollment() rolls back to Pending and sets an error on failure', () => {
    api.approve.and.returnValue(throwError(() => new Error('401')));
    configure();
    store.loadEnrollments();

    store.approveEnrollment(1);

    expect(statusOf(1)).toBe('Pending');
    expect(store.error()).toContain('Server rejected');
  });

  it('rejectEnrollment() rolls back to the previous status on failure', () => {
    api.reject.and.returnValue(throwError(() => new Error('401')));
    configure();
    store.loadEnrollments();

    store.rejectEnrollment({ id: 1, previous: 'Pending' });

    expect(statusOf(1)).toBe('Pending');
    expect(store.error()).toContain('Server rejected');
  });
});

import { computed, inject } from '@angular/core';
import {
  signalStore,
  withComputed,
  withMethods,
  patchState,
  withState,
} from '@ngrx/signals';
import {
  withEntities,
  setAllEntities,
  updateEntity,
} from '@ngrx/signals/entities';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, concatMap, tap, catchError, EMPTY } from 'rxjs';
import { EnrollmentService } from '../services/enrollment.service';
import { Enrollment, EnrollmentStatus } from '../models/enrollment.model';

export const EnrollmentStore = signalStore(
  { providedIn: 'root' },

  // withState: simple properties alongside the entity collection
  withState({ isLoading: false, error: null as string | null }),

  // withEntities: O(1) ID-indexed dictionary keyed by the numeric enrollment id
  // ({ ids: number[], entityMap: Record<number, Enrollment> }). selectId defaults
  // to the entity's `id` field, which is now the numeric PK from the API.
  withEntities<Enrollment>(),

  // withComputed: read-only derived signals that update automatically
  withComputed((store) => ({
    pendingCount: computed(
      () => store.entities().filter((e) => e.status === 'Pending').length,
    ),
    approvedCount: computed(
      () => store.entities().filter((e) => e.status === 'Approved').length,
    ),
    total: computed(() => store.entities().length),
  })),

  withMethods((store, api = inject(EnrollmentService)) => ({
    // Loading Data — concatMap processes one emission at a time in strict order,
    // so a rapid double-trigger waits for the first response instead of racing.
    loadEnrollments: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        concatMap(() =>
          api.getAll().pipe(
            tap((rows) =>
              patchState(store, setAllEntities(rows), { isLoading: false }),
            ),
            catchError((err) => {
              patchState(store, {
                isLoading: false,
                error: err.error?.detail ?? err.message,
              });
              return EMPTY; // EMPTY completes silently so the rxMethod pipeline survives
            }),
          ),
        ),
      ),
    ),

    // Optimistic Approve — flip status to "Approved" instantly, call the server,
    // roll back to "Pending" if it rejects. Every component reading the store
    // (queue table + summary KPIs) reacts before the round-trip completes.
    approveEnrollment: rxMethod<number>(
      pipe(
        tap((id) =>
          patchState(store, updateEntity({ id, changes: { status: 'Approved' } })),
        ),
        concatMap((id) =>
          api.approve(id).pipe(
            catchError(() => {
              patchState(store, updateEntity({ id, changes: { status: 'Pending' } }));
              patchState(store, {
                error: 'Server rejected the approval. Please sign in and try again.',
              });
              return EMPTY;
            }),
          ),
        ),
      ),
    ),

    // Optimistic Reject — same pattern, restoring the prior status on failure.
    rejectEnrollment: rxMethod<{ id: number; previous: EnrollmentStatus }>(
      pipe(
        tap(({ id }) =>
          patchState(store, updateEntity({ id, changes: { status: 'Rejected' } })),
        ),
        concatMap(({ id, previous }) =>
          api.reject(id).pipe(
            catchError(() => {
              patchState(store, updateEntity({ id, changes: { status: previous } }));
              patchState(store, {
                error: 'Server rejected the action. Please sign in and try again.',
              });
              return EMPTY;
            }),
          ),
        ),
      ),
    ),
  })),
);

import { inject } from '@angular/core';
import { signalStore, withMethods, withState, patchState } from '@ngrx/signals';
import { withEntities, setAllEntities, removeEntity } from '@ngrx/signals/entities';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, concatMap, tap, catchError, EMPTY } from 'rxjs';
import { CourseService } from '../services/course.service';
import { Course } from '../models/course.model';

// M10 Session 3 - Exercise 3 Part B: SignalStore with optimistic delete + rollback.
export const CourseStore = signalStore(
  { providedIn: 'root' },

  withState({ isLoading: false, error: null as string | null }),

  // withEntities<Course> — selectId defaults to the numeric `id`, giving an
  // O(1) map we can remove from and restore into for optimistic UI updates.
  withEntities<Course>(),

  withMethods((store, svc = inject(CourseService)) => ({
    loadCourses: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        concatMap(() =>
          svc.getAll().pipe(
            tap((rows) => patchState(store, setAllEntities(rows), { isLoading: false })),
            catchError((err) => {
              patchState(store, { isLoading: false, error: err.message });
              return EMPTY; // EMPTY completes silently so the rxMethod pipeline survives
            }),
          ),
        ),
      ),
    ),

    // --- M10 Session 3 - Exercise 3 Part B: optimistic delete with rollback ---
    deleteCourse(id: number) {
      // 1. Take a snapshot of current entities BEFORE mutating local state.
      //    Snapshotting AFTER removeEntity would already be missing the deleted
      //    row, so the rollback would be wrong (Execution Order Rule).
      const previousSnapshot = store.entities();

      // 2. Instant visual feedback — remove the entity immediately from local UI.
      patchState(store, removeEntity(id));

      // 3. Dispatch the API call to the backend server.
      svc
        .delete(id)
        .pipe(
          catchError(() => {
            // 4. Server rejected the request (e.g. 409 Conflict: active
            //    enrollments) — restore the previous snapshot and set an error.
            patchState(store, setAllEntities(previousSnapshot));
            patchState(store, {
              error: 'Cannot delete course: active student enrollments exist.',
            });
            return EMPTY;
          }),
        )
        .subscribe();
    },
  })),
);

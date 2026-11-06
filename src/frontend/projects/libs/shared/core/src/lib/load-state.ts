import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, map, Observable, of, startWith, switchMap } from 'rxjs';

export type LoadState<T> =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly value: T }
  | { readonly status: 'missing' }
  | { readonly status: 'forbidden' }
  | { readonly status: 'error' };

/** Turns a one-shot request into loading, then ready, missing (404), forbidden (403) or error. */
export function withLoadState<T>(source: Observable<T>): Observable<LoadState<T>> {
  return source.pipe(
    map((value): LoadState<T> => ({ status: 'ready', value })),
    catchError((error: unknown) => of<LoadState<T>>({ status: failureOf(error) })),
    startWith<LoadState<T>>({ status: 'loading' }),
  );
}

function failureOf(error: unknown): 'missing' | 'forbidden' | 'error' {
  if (error instanceof HttpErrorResponse) {
    if (error.status === HttpStatusCode.NotFound) {
      return 'missing';
    }
    if (error.status === HttpStatusCode.Forbidden) {
      return 'forbidden';
    }
  }

  return 'error';
}

export interface Reloadable<T> {
  readonly state: Signal<LoadState<T>>;
  /** Sends the request again, e.g. after a change. */
  reload(): void;
}

/** Loads at once and on every `reload()`. Call it in an injection context, e.g. a field. */
export function reloadable<T>(load: () => Observable<T>): Reloadable<T> {
  const trigger = new BehaviorSubject<void>(undefined);
  const state = toSignal(trigger.pipe(switchMap(() => withLoadState(load()))), {
    requireSync: true,
  });

  return { state, reload: () => trigger.next() };
}

/** The loaded value, or `fallback` while loading or after a failure. */
export function valueOr<T>(state: LoadState<T>, fallback: T): T {
  return state.status === 'ready' ? state.value : fallback;
}

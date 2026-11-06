import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError, toArray } from 'rxjs';
import { reloadable, valueOr, withLoadState } from './load-state';

describe('withLoadState', () => {
  it('starts loading, then holds the value', async () => {
    const states = await firstValueFrom(withLoadState(of(3)).pipe(toArray()));

    expect(states).toEqual([{ status: 'loading' }, { status: 'ready', value: 3 }]);
  });

  it.each([
    [404, 'missing'],
    [403, 'forbidden'],
    [500, 'error'],
  ])('maps a %s response to %s', async (status, expected) => {
    const failing = throwError(() => new HttpErrorResponse({ status }));

    const states = await firstValueFrom(withLoadState(failing).pipe(toArray()));

    expect(states[1]).toEqual({ status: expected });
  });

  it('treats other failures as errors', async () => {
    const states = await firstValueFrom(
      withLoadState(throwError(() => new Error('boom'))).pipe(toArray()),
    );

    expect(states[1]).toEqual({ status: 'error' });
  });
});

describe('reloadable', () => {
  it('loads at once and again on reload', () => {
    let calls = 0;
    const loaded = TestBed.runInInjectionContext(() => reloadable(() => of(++calls)));

    expect(loaded.state()).toEqual({ status: 'ready', value: 1 });

    loaded.reload();

    expect(loaded.state()).toEqual({ status: 'ready', value: 2 });
  });
});

describe('valueOr', () => {
  it('gives the value once ready, the fallback otherwise', () => {
    expect(valueOr({ status: 'ready', value: [1] }, [])).toEqual([1]);
    expect(valueOr<number[]>({ status: 'loading' }, [])).toEqual([]);
  });
});

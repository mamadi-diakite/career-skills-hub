import { TestBed } from '@angular/core/testing';
import { SESSION_STORAGE_KEY, SessionStore } from './session.store';
import { testSession } from './test-session';

const now = new Date('2026-11-06T10:00:00Z');

describe('SessionStore', () => {
  beforeEach(() => {
    vi.useFakeTimers({ now, toFake: ['Date', 'setTimeout', 'clearTimeout'] });
    localStorage.clear();
  });

  afterEach(() => vi.useRealTimers());

  it('starts with nobody signed in', () => {
    const store = TestBed.inject(SessionStore);

    expect(store.token()).toBeNull();
    expect(store.user()).toBeNull();
    expect(store.expired()).toBe(false);
  });

  it('keeps a started session in local storage until it is cleared', () => {
    const store = TestBed.inject(SessionStore);

    store.start(testSession('2026-11-06T11:00:00Z'));

    expect(store.token()).toBe('token-1');
    expect(store.user()?.displayName).toBe('Claire Fontaine');
    expect(JSON.parse(localStorage.getItem(SESSION_STORAGE_KEY)!)).toEqual(
      testSession('2026-11-06T11:00:00Z'),
    );

    store.clear();

    expect(store.token()).toBeNull();
    expect(store.expired()).toBe(false);
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('restores a stored session that has not expired, and ends it on time', () => {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(testSession('2026-11-06T10:30:00Z')));
    const store = TestBed.inject(SessionStore);

    expect(store.user()?.email).toBe('claire.fontaine@example.com');

    vi.advanceTimersByTime(30 * 60 * 1000);

    expect(store.user()).toBeNull();
    expect(store.expired()).toBe(true);
  });

  it.each([
    ['an expired session', JSON.stringify(testSession('2026-11-06T09:59:59Z'))],
    ['a session without token', JSON.stringify({ ...testSession(), accessToken: '' })],
    ['text that is not JSON', '{oops'],
  ])('ignores %s in storage', (_case, stored) => {
    localStorage.setItem(SESSION_STORAGE_KEY, stored);

    expect(TestBed.inject(SessionStore).token()).toBeNull();
  });

  it('ends the session when its expiry time is reached', () => {
    const store = TestBed.inject(SessionStore);
    store.start(testSession('2026-11-06T10:30:00Z'));

    vi.advanceTimersByTime(30 * 60 * 1000 - 1);
    expect(store.expired()).toBe(false);
    vi.advanceTimersByTime(1);

    expect(store.user()).toBeNull();
    expect(store.expired()).toBe(true);
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('treats a token read after its expiry as expired', () => {
    const store = TestBed.inject(SessionStore);
    store.start(testSession('2026-11-06T10:30:00Z'));

    vi.setSystemTime(new Date('2026-11-06T10:30:00Z'));

    expect(store.token()).toBeNull();
    expect(store.expired()).toBe(true);
  });

  it('waits again when the expiry is further than one timer can wait', () => {
    const store = TestBed.inject(SessionStore);
    store.start(testSession('2026-12-31T10:00:00Z'));

    vi.advanceTimersByTime(2_147_483_647);
    expect(store.token()).toBe('token-1');

    vi.advanceTimersByTime(55 * 24 * 60 * 60 * 1000);
    expect(store.expired()).toBe(true);
  });

  it('starts fresh after an expiry', () => {
    const store = TestBed.inject(SessionStore);
    store.start(testSession('2026-11-06T10:30:00Z'));
    vi.advanceTimersByTime(30 * 60 * 1000);

    store.start(testSession('2026-11-06T12:00:00Z'));

    expect(store.expired()).toBe(false);
    expect(store.token()).toBe('token-1');
  });

  it('stops its timer when destroyed', () => {
    const store = TestBed.inject(SessionStore);
    store.start(testSession('2026-11-06T10:30:00Z'));

    store.ngOnDestroy();
    vi.advanceTimersByTime(30 * 60 * 1000);

    expect(store.expired()).toBe(false);
  });
});

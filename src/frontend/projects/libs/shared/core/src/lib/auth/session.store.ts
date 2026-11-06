import { computed, Injectable, OnDestroy, signal } from '@angular/core';
import { LoginResponse } from './auth.models';

export const SESSION_STORAGE_KEY = 'career-skills-hub.session';

/** Longest delay `setTimeout` accepts; later expiries are checked again after it. */
const MAX_TIMER_DELAY = 2_147_483_647;

/** The signed-in user and their access token, kept in local storage until it expires. */
@Injectable({ providedIn: 'root' })
export class SessionStore implements OnDestroy {
  private readonly session = signal<LoginResponse | null>(readStoredSession());
  private readonly expiredFlag = signal(false);
  private timer: ReturnType<typeof setTimeout> | undefined;

  readonly user = computed(() => this.session()?.user ?? null);

  /** `true` once the session has ended because its token expired, until the next sign-in. */
  readonly expired = this.expiredFlag.asReadonly();

  constructor() {
    const session = this.session();
    if (session) {
      this.scheduleExpiry(session);
    }
  }

  /** The access token, or `null` when nobody is signed in or the session has expired. */
  token(): string | null {
    const session = this.session();
    if (session && isExpired(session)) {
      this.expire();
      return null;
    }

    return session?.accessToken ?? null;
  }

  start(session: LoginResponse): void {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
    this.expiredFlag.set(false);
    this.scheduleExpiry(session);
  }

  clear(): void {
    clearTimeout(this.timer);
    localStorage.removeItem(SESSION_STORAGE_KEY);
    this.session.set(null);
  }

  ngOnDestroy(): void {
    clearTimeout(this.timer);
  }

  private expire(): void {
    this.clear();
    this.expiredFlag.set(true);
  }

  private scheduleExpiry(session: LoginResponse): void {
    clearTimeout(this.timer);
    const delay = Math.min(Date.parse(session.expiresAtUtc) - Date.now(), MAX_TIMER_DELAY);
    this.timer = setTimeout(
      () => (isExpired(session) ? this.expire() : this.scheduleExpiry(session)),
      delay,
    );
  }
}

function isExpired(session: LoginResponse): boolean {
  return Date.parse(session.expiresAtUtc) <= Date.now();
}

function readStoredSession(): LoginResponse | null {
  try {
    const session = JSON.parse(
      localStorage.getItem(SESSION_STORAGE_KEY) ?? 'null',
    ) as LoginResponse | null;
    return session?.accessToken && !isExpired(session) ? session : null;
  } catch {
    return null;
  }
}

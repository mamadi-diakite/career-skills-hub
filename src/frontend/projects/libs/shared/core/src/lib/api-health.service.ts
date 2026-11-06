import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@career-skills-hub/shared/environment';
import { catchError, map, Observable, of } from 'rxjs';

export type ApiStatus = 'checking' | 'online' | 'offline';

@Injectable({ providedIn: 'root' })
export class ApiHealthService {
  private readonly http = inject(HttpClient);
  private readonly environment = inject(APP_ENVIRONMENT);

  /** Emits `online` when the API answers `ok`, `offline` otherwise. Never errors. */
  check(): Observable<ApiStatus> {
    return this.http.get<{ status?: string }>(`${this.environment.apiBaseUrl}/health`).pipe(
      map((body): ApiStatus => (body?.status === 'ok' ? 'online' : 'offline')),
      catchError(() => of<ApiStatus>('offline')),
    );
  }
}

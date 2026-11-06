import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SessionStore } from './session.store';

/**
 * Sends the access token, and sends the user back to login when the API refuses the session.
 * A 401 from the login itself only means wrong credentials.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const token = session.token();
  const sent = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(sent).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === HttpStatusCode.Unauthorized &&
        !request.url.endsWith('/auth/login')
      ) {
        session.clear();
        void router.navigate(['/login'], { queryParams: { expired: true } });
      }

      return throwError(() => error);
    }),
  );
};

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session.store';

/** Lets signed-in users through; sends others to login, then back to where they were going. */
export const authGuard: CanActivateFn = (_route, state) =>
  inject(SessionStore).token() !== null ||
  inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

/** Keeps signed-in users away from the login and sign-up pages. */
export const guestGuard: CanActivateFn = () =>
  inject(SessionStore).token() === null || inject(Router).createUrlTree(['/']);

/** Keeps a return address inside the app, so a link cannot send users to another site. */
export function safeReturnUrl(returnUrl: string | null | undefined): string {
  return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/';
}

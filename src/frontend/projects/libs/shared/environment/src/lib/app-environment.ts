import { InjectionToken, Provider } from '@angular/core';

export interface AppEnvironment {
  /** Base URL of the API without a trailing slash, e.g. `/api`. */
  readonly apiBaseUrl: string;
}

export const APP_ENVIRONMENT = new InjectionToken<AppEnvironment>('APP_ENVIRONMENT');

export function provideAppEnvironment(environment: AppEnvironment): Provider {
  return { provide: APP_ENVIRONMENT, useValue: environment };
}

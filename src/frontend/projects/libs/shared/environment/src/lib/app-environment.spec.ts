import { TestBed } from '@angular/core/testing';
import { APP_ENVIRONMENT, provideAppEnvironment } from './app-environment';

describe('provideAppEnvironment', () => {
  it('provides the environment through the token', () => {
    TestBed.configureTestingModule({ providers: [provideAppEnvironment({ apiBaseUrl: '/api' })] });

    expect(TestBed.inject(APP_ENVIRONMENT)).toEqual({ apiBaseUrl: '/api' });
  });
});

import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { APP_ENVIRONMENT } from '@career-skills-hub/shared/environment';
import { App } from './app';
import { appConfig } from './app.config';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the API behind the same origin', () => {
    expect(TestBed.inject(APP_ENVIRONMENT).apiBaseUrl).toBe('/api');
  });

  it('shows the home page and the API status once checked', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h1')?.textContent).toBe('The skills your teams have and need');
    expect(element.querySelector('[data-testid="api-status"]')?.textContent).toBe('API checking…');

    http.expectOne('/api/health').flush({ status: 'ok' });
    await fixture.whenStable();

    expect(element.querySelector('[data-testid="api-status"]')?.textContent).toBe('API online');
  });

  it('sends unknown addresses back home', async () => {
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/does-not-exist');

    expect(router.url).toBe('/');
  });
});

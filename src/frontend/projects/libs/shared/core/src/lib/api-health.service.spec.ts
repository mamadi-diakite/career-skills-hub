import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@career-skills-hub/shared/environment';
import { firstValueFrom } from 'rxjs';
import { ApiHealthService } from './api-health.service';

describe('ApiHealthService', () => {
  let service: ApiHealthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(ApiHealthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reports online when the API answers ok', async () => {
    const status = firstValueFrom(service.check());

    http.expectOne('/api/health').flush({ status: 'ok' });

    expect(await status).toBe('online');
  });

  it.each([{ status: 'degraded' }, {}, null])(
    'reports offline for an unexpected body (%o)',
    async (body) => {
      const status = firstValueFrom(service.check());

      http.expectOne('/api/health').flush(body);

      expect(await status).toBe('offline');
    },
  );

  it('reports offline when the request fails', async () => {
    const status = firstValueFrom(service.check());

    http.expectOne('/api/health').flush(null, { status: 502, statusText: 'Bad Gateway' });

    expect(await status).toBe('offline');
  });
});

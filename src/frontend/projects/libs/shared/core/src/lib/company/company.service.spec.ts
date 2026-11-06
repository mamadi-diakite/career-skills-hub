import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@career-skills-hub/shared/environment';
import { firstValueFrom } from 'rxjs';
import { CompanyService } from './company.service';

const company = { id: 'company-1', name: 'Blue Harbour Logistics', headcount: 64 };

describe('CompanyService', () => {
  let service: CompanyService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(CompanyService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the company profile', async () => {
    const result = firstValueFrom(service.get());

    http.expectOne('/api/company').flush(company);

    expect(await result).toEqual(company);
  });

  it('updates the company profile', async () => {
    const result = firstValueFrom(service.update({ name: 'Blue Harbour Group', headcount: 72 }));

    const request = http.expectOne('/api/company');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Blue Harbour Group', headcount: 72 });
    request.flush({ ...company, name: 'Blue Harbour Group', headcount: 72 });

    expect((await result).headcount).toBe(72);
  });
});

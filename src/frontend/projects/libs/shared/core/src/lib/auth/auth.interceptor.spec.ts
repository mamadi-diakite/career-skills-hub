import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { SessionStore } from './session.store';
import { testSession } from './test-session';

describe('authInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let session: SessionStore;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(SessionStore);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  it('sends requests unchanged when nobody is signed in', () => {
    client.get('/api/jobs').subscribe();

    expect(http.expectOne('/api/jobs').request.headers.has('Authorization')).toBe(false);
  });

  it('adds the access token of the signed-in user', () => {
    session.start(testSession());
    client.get('/api/jobs').subscribe();

    expect(http.expectOne('/api/jobs').request.headers.get('Authorization')).toBe('Bearer token-1');
  });

  it.each([
    ['the token is refused', true],
    ['the request had no token left', false],
  ])('ends the session and goes to login when %s', async (_case, signedIn) => {
    if (signedIn) {
      session.start(testSession());
    }
    const result = firstValueFrom(client.get('/api/jobs'));

    http.expectOne('/api/jobs').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toMatchObject({ status: 401 });
    expect(session.token()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { expired: true } });
  });

  it('leaves a refused login to the login page', async () => {
    const result = firstValueFrom(client.post('/api/auth/login', {}));

    http.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toMatchObject({ status: 401 });
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('passes other errors through and keeps the session', async () => {
    session.start(testSession());
    const result = firstValueFrom(client.get('/api/jobs'));

    http.expectOne('/api/jobs').flush(null, { status: 404, statusText: 'Not Found' });

    await expect(result).rejects.toMatchObject({ status: 404 });
    expect(session.token()).toBe('token-1');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('passes errors that are not HTTP responses through', async () => {
    const result = firstValueFrom(client.get('/api/jobs'));

    http.expectOne('/api/jobs').error(new ProgressEvent('error'));

    await expect(result).rejects.toMatchObject({ status: 0 });
    expect(router.navigate).not.toHaveBeenCalled();
  });
});

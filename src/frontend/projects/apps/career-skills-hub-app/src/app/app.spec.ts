import { TestBed } from '@angular/core/testing';
import { APP_ENVIRONMENT } from '@career-skills-hub/shared/environment';
import { AppHarness } from '../testing/app-harness';

describe('App', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  it('uses the API behind the same origin', () => {
    expect(TestBed.inject(APP_ENVIRONMENT).apiBaseUrl).toBe('/api');
  });

  it('shows the home page, the API status and the visitor links', async () => {
    await app.open('/');

    expect(app.text('h1')).toBe('The skills your teams have and need');
    expect(app.text('[data-testid="api-status"]')).toBe('API online');
    expect(app.texts('header nav a')).toEqual(['Log in', 'Create your company']);
  });

  it.each([
    ['HrAdmin', 'Claire Fontaine · HR admin'],
    ['Manager', 'Marc Lefebvre · Manager'],
    ['Employee', 'Ines Moreau · Employee'],
  ] as const)('shows the signed-in %s with their role', async (role, label) => {
    app.signIn(role);
    await app.open('/');

    expect(app.text('[data-testid="user-name"]')).toBe(label);
  });

  it('logs out to the login page', async () => {
    app.signIn();
    await app.open('/');
    expect(app.texts('header nav a')).toEqual([
      'Employees',
      'Jobs',
      'Skills',
      'Departments',
      'Company',
      'Change password',
    ]);

    await app.click('header nav button');

    expect(app.router.url).toBe('/login');
    expect(app.texts('header nav a')).toEqual(['Log in', 'Create your company']);
  });

  it('sends the user to login when the session expires', async () => {
    app.signIn('Employee', new Date(Date.now() + 20).toISOString());
    await app.open('/account/password');

    await new Promise((resolve) => setTimeout(resolve, 40));
    await app.settle();

    expect(app.router.url).toBe('/login?expired=true');
    expect(app.text('[role="status"]')).toBe('Your session has expired. Please log in again.');
    expect(app.texts('header nav a')).toEqual(['Log in', 'Create your company']);
  });

  it('sends unknown addresses back home', async () => {
    await app.open('/does-not-exist');

    expect(app.router.url).toBe('/');
  });
});

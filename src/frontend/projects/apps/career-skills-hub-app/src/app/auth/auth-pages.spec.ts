import { AppHarness, sessionFor } from '../../testing/app-harness';

describe('Auth pages', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  describe('login', () => {
    it('asks for the missing fields without calling the API', async () => {
      await app.open('/login');

      await app.submit();

      expect(app.texts('csh-field-errors p')).toEqual([
        'Enter a valid email address.',
        'Enter your password.',
      ]);
    });

    it('shows the API message when the credentials are wrong', async () => {
      await app.open('/login');
      await app.type('email', 'claire.fontaine@example.com');
      await app.type('password', 'wrong');

      await app.submit();
      expect(app.button().disabled).toBe(true);
      const request = app.http.expectOne('/api/auth/login');
      expect(request.request.body).toEqual({
        email: 'claire.fontaine@example.com',
        password: 'wrong',
      });
      request.flush(
        { title: 'Email or password is not correct.' },
        { status: 401, statusText: 'Unauthorized' },
      );
      await app.settle();

      expect(app.router.url).toBe('/login');
      expect(app.text('[role="alert"]')).toBe('Email or password is not correct.');
      expect(app.button().disabled).toBe(false);
    });

    it('signs in and goes back to the page the user wanted', async () => {
      await app.open('/account/password');
      expect(app.router.url).toBe('/login?returnUrl=%2Faccount%2Fpassword');

      await app.type('email', 'marc.lefebvre@example.com');
      await app.type('password', 'CareerDemo123!');
      await app.submit();
      app.http.expectOne('/api/auth/login').flush(sessionFor('Manager'));
      await app.settle();

      expect(app.router.url).toBe('/account/password');
      expect(app.text('[data-testid="user-name"]')).toBe('Marc Lefebvre · Manager');
    });

    it('goes home when the return address leaves the app', async () => {
      await app.open('/login?returnUrl=%2F%2Fevil.example.com');
      await app.type('email', 'claire.fontaine@example.com');
      await app.type('password', 'CareerDemo123!');

      await app.submit();
      app.http.expectOne('/api/auth/login').flush(sessionFor('HrAdmin'));
      await app.settle();

      expect(app.router.url).toBe('/');
    });

    it('explains that the session has expired', async () => {
      await app.open('/login?expired=true');

      expect(app.text('[role="status"]')).toBe('Your session has expired. Please log in again.');
    });

    it('is not shown to signed-in users', async () => {
      app.signIn();

      await app.open('/login');

      expect(app.router.url).toBe('/');
    });
  });

  describe('sign-up', () => {
    async function fill(): Promise<void> {
      await app.open('/sign-up');
      await app.type('companyName', 'Orchard Tools');
      await app.type('headcount', '58');
      await app.type('displayName', 'Nina Perrin');
      await app.type('email', 'nina.perrin@example.com');
      await app.type('password', 'short');
      await app.type('confirmPassword', 'other');
    }

    it('shows the API messages next to each field', async () => {
      await fill();

      await app.submit();
      app.http.expectOne('/api/auth/sign-up').flush(
        {
          errors: {
            password: ['Use at least 8 characters.'],
            confirmPassword: ['Confirmation does not match the password.'],
          },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      await app.settle();

      expect(app.texts('csh-field-errors p')).toEqual([
        'Use at least 8 characters.',
        'Confirmation does not match the password.',
      ]);
    });

    it('creates the company and its HR admin, signs in and goes home', async () => {
      await fill();

      await app.submit();
      const request = app.http.expectOne('/api/auth/sign-up');
      expect(request.request.body).toEqual({
        companyName: 'Orchard Tools',
        headcount: 58,
        displayName: 'Nina Perrin',
        email: 'nina.perrin@example.com',
        password: 'short',
        confirmPassword: 'other',
      });
      request.flush(sessionFor('HrAdmin'), { status: 201, statusText: 'Created' });
      await app.settle();

      expect(app.router.url).toBe('/');
      expect(app.text('[data-testid="user-name"]')).toBe('Claire Fontaine · HR admin');
    });

    it('shows a general message when the API fails', async () => {
      await fill();

      await app.submit();
      app.http
        .expectOne('/api/auth/sign-up')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('asks for every field when empty', async () => {
      await app.open('/sign-up');

      await app.submit();

      expect(app.texts('csh-field-errors p')).toEqual([
        'Enter the company name.',
        'Enter the number of employees, at least 1.',
        'Enter your name.',
        'Enter a valid email address.',
        'Choose a password.',
        'Confirm your password.',
      ]);
    });

    it('refuses a headcount below 1', async () => {
      await app.open('/sign-up');

      await app.type('headcount', '0');

      expect(app.texts('csh-field-errors p')).toEqual([
        'Enter the number of employees, at least 1.',
      ]);
    });
  });

  describe('change password', () => {
    beforeEach(async () => {
      app.signIn('Employee');
      await app.open('/account/password');
      await app.type('currentPassword', 'CareerDemo123!');
      await app.type('newPassword', 'NewPass456');
      await app.type('confirmPassword', 'NewPass456');
    });

    it('shows the API messages next to each field', async () => {
      await app.submit();
      const request = app.http.expectOne('/api/auth/change-password');
      expect(request.request.headers.get('Authorization')).toBe('Bearer token-Employee');
      request.flush(
        { errors: { currentPassword: ['Current password is not correct.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
      await app.settle();

      expect(app.texts('csh-field-errors p')).toEqual(['Current password is not correct.']);
      expect(app.element.querySelector('[role="status"]')).toBeNull();
    });

    it('confirms the change and clears the form', async () => {
      await app.submit();
      app.http
        .expectOne('/api/auth/change-password')
        .flush(null, { status: 204, statusText: 'No Content' });
      await app.settle();

      expect(app.text('[role="status"]')).toBe('Your password has been changed.');
      expect(app.input('currentPassword').value).toBe('');
      expect(app.texts('csh-field-errors p')).toEqual([]);
    });

    it('asks for every field when empty', async () => {
      await app.type('currentPassword', '');
      await app.type('newPassword', '');
      await app.type('confirmPassword', '');

      await app.submit();

      expect(app.texts('csh-field-errors p')).toEqual([
        'Enter your current password.',
        'Choose a new password.',
        'Confirm your new password.',
      ]);
    });

    it('shows a general message when the API fails', async () => {
      await app.submit();
      app.http
        .expectOne('/api/auth/change-password')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('sends the user to login when the session is refused', async () => {
      await app.submit();
      app.http
        .expectOne('/api/auth/change-password')
        .flush(null, { status: 401, statusText: 'Unauthorized' });
      await app.settle();

      expect(app.router.url).toBe('/login?expired=true');
      expect(app.text('[role="status"]')).toBe('Your session has expired. Please log in again.');
    });
  });
});

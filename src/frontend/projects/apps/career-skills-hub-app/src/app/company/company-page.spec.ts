import { AppHarness } from '../../testing/app-harness';

const company = { id: 'company-1', name: 'Blue Harbour Logistics', headcount: 64 };

describe('CompanyPage', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openAs(role: 'HrAdmin' | 'Manager' | 'Employee'): Promise<void> {
    app.signIn(role);
    await app.open('/company');
    app.http.expectOne('/api/company').flush(company);
    await app.settle();
  }

  it('needs a signed-in user', async () => {
    await app.open('/company');

    expect(app.router.url).toBe('/login?returnUrl=%2Fcompany');
  });

  it('lets HR change the name and the headcount', async () => {
    await openAs('HrAdmin');
    expect(app.input('name').value).toBe('Blue Harbour Logistics');
    expect(app.input('headcount').value).toBe('64');

    await app.type('name', 'Blue Harbour Group');
    await app.type('headcount', '72');
    await app.submit();
    const request = app.http.expectOne('/api/company');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Blue Harbour Group', headcount: 72 });
    request.flush({ ...company, name: 'Blue Harbour Group', headcount: 72 });
    await app.settle();

    expect(app.text('[role="status"]')).toBe('The company profile has been saved.');
    expect(app.input('name').value).toBe('Blue Harbour Group');
  });

  it('checks the fields before saving', async () => {
    await openAs('HrAdmin');

    await app.type('name', '');
    await app.type('headcount', '');
    await app.submit();

    expect(app.texts('csh-field-errors p')).toEqual([
      'Enter the company name.',
      'Enter the number of employees, at least 1.',
    ]);
  });

  it('shows the API messages', async () => {
    await openAs('HrAdmin');

    await app.submit();
    app.http
      .expectOne('/api/company')
      .flush(
        { errors: { headcount: ['Headcount must be between 1 and 1000000.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();

    expect(app.texts('csh-field-errors p')).toEqual(['Headcount must be between 1 and 1000000.']);
    expect(app.element.querySelector('[role="status"]')).toBeNull();
  });

  it('shows why the API refused the change', async () => {
    await openAs('HrAdmin');

    await app.submit();
    app.http
      .expectOne('/api/company')
      .flush(
        { title: 'Only HR admins can edit the company profile.' },
        { status: 403, statusText: 'Forbidden' },
      );
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Only HR admins can edit the company profile.');
  });

  it.each(['Manager', 'Employee'] as const)('shows the profile read-only to a %s', async (role) => {
    await openAs(role);

    expect(app.input('name').disabled).toBe(true);
    expect(app.input('headcount').value).toBe('64');
    expect(app.element.querySelector('button[type="submit"]')).toBeNull();
    expect(app.text('form p')).toBe('Only HR admins can change the company profile.');
  });

  it('tells when the profile cannot be loaded', async () => {
    app.signIn('HrAdmin');
    await app.open('/company');

    app.http.expectOne('/api/company').flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The company profile could not be loaded.');
  });
});

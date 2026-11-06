import { EmployeeDetails } from '@career-skills-hub/shared/core';
import { AppHarness } from '../../testing/app-harness';
import { departments, ines, inesDetails, jobs, marc } from '../../testing/fixtures';

const jobChangeForm = '[data-testid="job-change-form"]';
const invitationForm = '[data-testid="invitation-form"]';
const detailValues = '[data-testid="employee-details"] dd';

describe('EmployeePage', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openAs(
    role: 'HrAdmin' | 'Manager' | 'Employee',
    details: EmployeeDetails = inesDetails,
  ): Promise<void> {
    app.signIn(role);
    await app.open(`/employees/${details.employee.id}`);
    expect(app.text('[role="status"]')).toBe('Loading the employee…');
    await flushDetails(details);
    app.http.expectOne('/api/jobs').flush(jobs);
    app.http.expectOne('/api/departments').flush(departments);
    await app.settle();
  }

  async function flushDetails(details: EmployeeDetails = inesDetails): Promise<void> {
    app.http
      .expectOne({ method: 'GET', url: `/api/employees/${details.employee.id}` })
      .flush(details);
    await app.settle();
  }

  async function openFailing(status: number): Promise<void> {
    app.signIn('Employee');
    await app.open('/employees/employee-other');
    app.http.expectOne('/api/employees/employee-other').flush(null, { status, statusText: 'No' });
    app.http.expectOne('/api/jobs').flush(jobs);
    app.http.expectOne('/api/departments').flush(departments);
    await app.settle();
  }

  it('needs a signed-in user', async () => {
    await app.open(`/employees/${ines.id}`);

    expect(app.router.url).toBe(`/login?returnUrl=%2Femployees%2F${ines.id}`);
  });

  it.each(['Manager', 'Employee'] as const)(
    'shows the record and the job history to a %s',
    async (role) => {
      await openAs(role);

      expect(app.text('h1')).toBe('Ines Moreau');
      expect(app.texts(detailValues)).toEqual([
        'ines.moreau@example.com',
        '2022-09-01',
        'Dispatcher',
        'Operations',
        'Marc Lefebvre',
        'Not invited yet',
      ]);
      expect(app.texts('[data-testid="assignment"] td')).toEqual([
        ...['Dispatcher', 'Operations', '2024-01-01', 'Today'],
        ...['Warehouse operator', 'Operations', '2022-09-01', '2023-12-31'],
      ]);
      expect(app.element.querySelector('form')).toBeNull();
    },
  );

  it('shows an employee without manager who has an account', async () => {
    await openAs('HrAdmin', { employee: marc, jobHistory: [] });

    expect(app.texts(detailValues).slice(4)).toEqual(['No manager', 'Manager']);
    expect(app.element.querySelector(jobChangeForm)).not.toBeNull();
    expect(app.element.querySelector(invitationForm)).toBeNull();
  });

  it('tells when the record is outside what the user may see', async () => {
    await openFailing(403);

    expect(app.text('[role="alert"]')).toBe('You can only see your own record and your team.');
  });

  it('tells when the employee does not exist', async () => {
    await openFailing(404);

    expect(app.text('[role="alert"]')).toBe('This employee does not exist.');
  });

  it('tells when the employee cannot be loaded', async () => {
    await openFailing(500);

    expect(app.text('[role="alert"]')).toBe('The employee could not be loaded.');
  });

  it('lets HR record a job change', async () => {
    await openAs('HrAdmin');

    await app.select('jobId', 'job-lead');
    await app.select('departmentId', 'department-ops');
    await app.type('effectiveDate', '2026-01-05');
    await app.submit(jobChangeForm);
    const request = app.http.expectOne(`/api/employees/${ines.id}/job-changes`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      jobId: 'job-lead',
      departmentId: 'department-ops',
      effectiveDate: '2026-01-05',
    });
    request.flush(inesDetails);
    await app.settle();
    await flushDetails();

    expect(app.text('[role="status"]')).toBe('The job change has been recorded.');
    expect(app.input('effectiveDate').value).toBe('');
  });

  it('checks the job change fields', async () => {
    await openAs('HrAdmin');

    await app.submit(jobChangeForm);

    expect(app.texts(`${jobChangeForm} csh-field-errors p`)).toEqual([
      'Choose the new job.',
      'Choose a department.',
      'Enter the date of the change.',
    ]);
  });

  it('shows why the API refused the job change', async () => {
    await openAs('HrAdmin');

    await app.select('jobId', 'job-lead');
    await app.select('departmentId', 'department-ops');
    await app.type('effectiveDate', '2020-01-01');
    await app.submit(jobChangeForm);
    app.http
      .expectOne(`/api/employees/${ines.id}/job-changes`)
      .flush(
        { title: 'The change must start after the current job.' },
        { status: 409, statusText: 'Conflict' },
      );
    await app.settle();

    expect(app.text(`${jobChangeForm} [role="alert"]`)).toBe(
      'The change must start after the current job.',
    );
  });

  it('lets HR invite the employee and shows the temporary password once', async () => {
    await openAs('HrAdmin');
    expect(app.texts('#role option')).toEqual(['Employee', 'Manager', 'HR admin']);

    await app.select('role', 'Manager');
    await app.submit(invitationForm);
    const request = app.http.expectOne(`/api/employees/${ines.id}/invitation`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ role: 'Manager' });
    request.flush({
      employeeId: ines.id,
      email: ines.email,
      role: 'Manager',
      temporaryPassword: 'abcdef-1234-ghijkl',
    });
    await app.settle();
    await flushDetails({ ...inesDetails, employee: { ...ines, accountRole: 'Manager' } });

    expect(app.text('[data-testid="invitation"] p')).toContain(
      'An account has been created for ines.moreau@example.com as Manager.',
    );
    expect(app.text('[data-testid="temporary-password"]')).toBe('abcdef-1234-ghijkl');
    expect(app.texts(detailValues).at(-1)).toBe('Manager');
    expect(app.element.querySelector(invitationForm)).toBeNull();
  });

  it('shows why the API refused the invitation', async () => {
    await openAs('HrAdmin');

    await app.submit(invitationForm);
    app.http
      .expectOne(`/api/employees/${ines.id}/invitation`)
      .flush(
        { title: 'This employee already has an account.' },
        { status: 409, statusText: 'Conflict' },
      );
    await app.settle();

    expect(app.text(`${invitationForm} [role="alert"]`)).toBe(
      'This employee already has an account.',
    );
    expect(app.element.querySelector('[data-testid="invitation"]')).toBeNull();
  });
});

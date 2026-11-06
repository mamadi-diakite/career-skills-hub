import { EmployeeSummary } from '@career-skills-hub/shared/core';
import { AppHarness } from '../../testing/app-harness';
import { departments, employees, ines, inesDetails, jobs } from '../../testing/fixtures';

describe('EmployeesPage', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openAs(
    role: 'HrAdmin' | 'Manager' | 'Employee',
    list: EmployeeSummary[] = employees,
  ): Promise<void> {
    app.signIn(role);
    await app.open('/employees');
    expect(app.text('[role="status"]')).toBe('Loading the employees…');
    app.http.expectOne('/api/employees').flush(list);
    app.http.expectOne('/api/jobs').flush(jobs);
    app.http.expectOne('/api/departments').flush(departments);
    await app.settle();
  }

  async function fill(): Promise<void> {
    await app.type('firstName', 'Lucas');
    await app.type('lastName', 'Bernard');
    await app.type('email', 'lucas.bernard@example.com');
    await app.type('hireDate', '2025-02-03');
    await app.select('jobId', 'job-lead');
    await app.select('departmentId', 'department-ops');
  }

  it('needs a signed-in user', async () => {
    await app.open('/employees');

    expect(app.router.url).toBe('/login?returnUrl=%2Femployees');
  });

  it.each([
    ['HrAdmin', 'Everyone in the company.'],
    ['Manager', 'You and your team.'],
    ['Employee', 'Your own record.'],
  ] as const)('tells a %s whom the list shows', async (role, intro) => {
    await openAs(role);

    expect(app.text('h1 + p')).toBe(intro);
    expect(app.texts('[data-testid="employee"]')).toEqual([
      'Marc Lefebvre Warehouse lead · Operations',
      'Ines Moreau Dispatcher · Operations',
    ]);
  });

  it('shows no form to a manager', async () => {
    await openAs('Manager');

    expect(app.element.querySelector('form')).toBeNull();
  });

  it('opens an employee', async () => {
    await openAs('Employee', [ines]);

    await app.click('[data-testid="employee"] a');

    expect(app.router.url).toBe(`/employees/${ines.id}`);
    app.http.expectOne(`/api/employees/${ines.id}`).flush(inesDetails);
    app.http.expectOne('/api/jobs').flush(jobs);
    app.http.expectOne('/api/departments').flush(departments);
  });

  it('says when there are no employees', async () => {
    await openAs('HrAdmin', []);

    expect(app.text('ul li')).toBe('No employees yet.');
  });

  it('tells when the employees cannot be loaded', async () => {
    app.signIn('Employee');
    await app.open('/employees');
    app.http.expectOne('/api/employees').flush(null, { status: 500, statusText: 'Error' });
    app.http.expectOne('/api/jobs').flush(jobs);
    app.http.expectOne('/api/departments').flush(departments);
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The employees could not be loaded.');
  });

  it('lets HR add an employee with a manager', async () => {
    await openAs('HrAdmin');
    expect(app.texts('#managerId option')).toEqual(['No manager', 'Marc Lefebvre', 'Ines Moreau']);

    await fill();
    await app.select('managerId', 'employee-marc');
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/employees' });
    expect(request.request.body).toEqual({
      firstName: 'Lucas',
      lastName: 'Bernard',
      email: 'lucas.bernard@example.com',
      hireDate: '2025-02-03',
      jobId: 'job-lead',
      departmentId: 'department-ops',
      managerId: 'employee-marc',
    });
    request.flush({ ...inesDetails, employee: { ...ines, id: 'employee-lucas' } });
    await app.settle();

    expect(app.router.url).toBe('/employees/employee-lucas');
    app.http.expectOne('/api/employees/employee-lucas').flush(inesDetails);
    app.http.expectOne('/api/jobs').flush(jobs);
    app.http.expectOne('/api/departments').flush(departments);
  });

  it('sends no manager when none is chosen', async () => {
    await openAs('HrAdmin');

    await fill();
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/employees' });
    expect(request.request.body.managerId).toBeNull();
    request.flush(
      { title: 'An employee with this email already exists.' },
      { status: 409, statusText: 'Conflict' },
    );
    await app.settle();

    expect(app.text('form [role="alert"]')).toBe('An employee with this email already exists.');
  });

  it('checks the fields before adding an employee', async () => {
    await openAs('HrAdmin');

    await app.type('email', 'not an email');
    await app.submit();

    expect(app.texts('csh-field-errors p')).toEqual([
      'Enter the first name.',
      'Enter the last name.',
      'Enter a valid email address.',
      'Enter the hire date.',
      'Choose a job.',
      'Choose a department.',
    ]);
  });
});

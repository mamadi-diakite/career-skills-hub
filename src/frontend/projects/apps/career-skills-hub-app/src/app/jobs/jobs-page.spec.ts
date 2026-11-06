import { AppHarness } from '../../testing/app-harness';
import { departments, dispatcher, jobs } from '../../testing/fixtures';

describe('JobsPage', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openAs(role: 'HrAdmin' | 'Manager' | 'Employee', list = jobs): Promise<void> {
    app.signIn(role);
    await app.open('/jobs');
    expect(app.text('[role="status"]')).toBe('Loading the jobs…');
    app.http.expectOne('/api/jobs').flush(list);
    app.http.expectOne('/api/departments').flush(departments);
    await app.settle();
  }

  it('needs a signed-in user', async () => {
    await app.open('/jobs');

    expect(app.router.url).toBe('/login?returnUrl=%2Fjobs');
  });

  it.each(['Manager', 'Employee'] as const)('lists the jobs read-only to a %s', async (role) => {
    await openAs(role);

    expect(app.texts('[data-testid="job"]')).toEqual([
      'Dispatcher Operations · 1 required skills',
      'Warehouse lead Operations · 0 required skills',
    ]);
    expect(app.element.querySelector('form')).toBeNull();
  });

  it('opens a job', async () => {
    await openAs('Employee');

    await app.click('[data-testid="job"] a');

    expect(app.router.url).toBe(`/jobs/${dispatcher.id}`);
    app.http.expectOne(`/api/jobs/${dispatcher.id}`).flush(dispatcher);
    app.http.expectOne('/api/departments').flush(departments);
    app.http.expectOne('/api/skill-domains').flush([]);
  });

  it('says when there are no jobs', async () => {
    await openAs('Employee', []);

    expect(app.text('ul li')).toBe('No jobs yet.');
  });

  it('tells when the jobs cannot be loaded', async () => {
    app.signIn('Employee');
    await app.open('/jobs');
    app.http.expectOne('/api/jobs').flush(null, { status: 500, statusText: 'Error' });
    app.http.expectOne('/api/departments').flush(departments);
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The jobs could not be loaded.');
  });

  it('lets HR add a job and opens it', async () => {
    await openAs('HrAdmin');
    expect(app.texts('#departmentId option')).toEqual([
      'Choose a department',
      'Operations',
      'People',
    ]);

    await app.type('title', 'Recruiter');
    await app.select('departmentId', 'department-people');
    await app.type('description', 'Hires new people.');
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/jobs' });
    expect(request.request.body).toEqual({
      title: 'Recruiter',
      departmentId: 'department-people',
      description: 'Hires new people.',
    });
    request.flush({ ...dispatcher, id: 'job-recruiter' });
    await app.settle();

    expect(app.router.url).toBe('/jobs/job-recruiter');
    app.http.expectOne('/api/jobs/job-recruiter').flush(dispatcher);
    app.http.expectOne('/api/departments').flush(departments);
    app.http.expectOne('/api/skill-domains').flush([]);
  });

  it('sends an empty description as none', async () => {
    await openAs('HrAdmin');

    await app.type('title', 'Recruiter');
    await app.select('departmentId', 'department-people');
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/jobs' });
    expect(request.request.body.description).toBeNull();
    request.flush({ title: 'This job already exists.' }, { status: 409, statusText: 'Conflict' });
    await app.settle();

    expect(app.text('form [role="alert"]')).toBe('This job already exists.');
  });

  it('checks the title and the department', async () => {
    await openAs('HrAdmin');

    await app.submit();

    expect(app.texts('csh-field-errors p')).toEqual([
      'Enter the title of the job.',
      'Choose a department.',
    ]);
  });
});

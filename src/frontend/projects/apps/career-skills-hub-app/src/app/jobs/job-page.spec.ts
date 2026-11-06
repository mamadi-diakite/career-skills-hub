import { Job } from '@career-skills-hub/shared/core';
import { AppHarness } from '../../testing/app-harness';
import {
  departments,
  dispatcher,
  forkliftDriving,
  jobs,
  routePlanning,
  skillDomains,
  warehouseLead,
} from '../../testing/fixtures';

const requiredForm = '[data-testid="required-skills-form"]';
const jobForm = '[data-testid="job-form"]';
const rows = '[data-testid="required-row"]';

describe('JobPage', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openAs(role: 'HrAdmin' | 'Manager' | 'Employee', job = dispatcher): Promise<void> {
    app.signIn(role);
    await app.open(`/jobs/${job.id}`);
    expect(app.text('[role="status"]')).toBe('Loading the job…');
    app.http.expectOne(`/api/jobs/${job.id}`).flush(job);
    app.http.expectOne('/api/departments').flush(departments);
    app.http.expectOne('/api/skill-domains').flush(skillDomains);
    await app.settle();
  }

  async function flushJob(job: Job = dispatcher): Promise<void> {
    app.http.expectOne({ method: 'GET', url: `/api/jobs/${job.id}` }).flush(job);
    await app.settle();
  }

  function rowSelects(index: number): HTMLSelectElement[] {
    return [...app.element.querySelectorAll(rows)[index].querySelectorAll('select')];
  }

  async function choose(select: HTMLSelectElement, value: string): Promise<void> {
    select.value = value;
    select.dispatchEvent(new Event('change'));
    await app.settle();
  }

  it('needs a signed-in user', async () => {
    await app.open(`/jobs/${dispatcher.id}`);

    expect(app.router.url).toBe(`/login?returnUrl=%2Fjobs%2F${dispatcher.id}`);
  });

  it.each(['Manager', 'Employee'] as const)(
    'shows the job and its required skills to a %s',
    async (role) => {
      await openAs(role);

      expect(app.text('h1')).toBe('Dispatcher');
      expect(app.text('[data-testid="department"]')).toBe('Operations');
      expect(app.text('[data-testid="description"]')).toBe('Plans the daily rounds.');
      expect(app.texts('[data-testid="required-skill"] td')).toEqual([
        'Route planning',
        'Advanced',
        'High',
      ]);
      expect(app.element.querySelector('form')).toBeNull();
    },
  );

  it('says when a job has no required skills', async () => {
    await openAs('Employee', warehouseLead);

    expect(app.element.querySelector('[data-testid="description"]')).toBeNull();
    expect(app.text('tbody td')).toBe('No required skills yet.');
  });

  it('tells when the job does not exist', async () => {
    app.signIn('Employee');
    await app.open('/jobs/job-unknown');
    app.http.expectOne('/api/jobs/job-unknown').flush(null, { status: 404, statusText: 'Nf' });
    app.http.expectOne('/api/departments').flush(departments);
    app.http.expectOne('/api/skill-domains').flush(skillDomains);
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('This job does not exist.');
  });

  it('tells when the job cannot be loaded', async () => {
    app.signIn('Employee');
    await app.open('/jobs/job-unknown');
    app.http.expectOne('/api/jobs/job-unknown').flush(null, { status: 500, statusText: 'Error' });
    app.http.expectOne('/api/departments').flush(departments);
    app.http.expectOne('/api/skill-domains').flush(skillDomains);
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The job could not be loaded.');
  });

  it('lets HR set the required skills', async () => {
    await openAs('HrAdmin');
    expect(rowSelects(0).map((select) => select.value)).toEqual([
      routePlanning.id,
      'Advanced',
      'High',
    ]);
    expect(app.texts(`${rows} select:first-child optgroup`).length).toBe(2);
    expect(app.texts(`${rows} select:first-child option`)).toEqual([
      'Choose a skill',
      'Forklift driving',
      'Route planning',
    ]);

    await app.click(`${requiredForm} button.button-secondary`);
    expect(rowSelects(1).map((select) => select.value)).toEqual(['', 'Intermediate', 'Medium']);
    const [skill, level, criticality] = rowSelects(1);
    await choose(skill, forkliftDriving.id);
    await choose(level, 'Expert');
    await choose(criticality, 'Low');
    await app.submit(requiredForm);
    const request = app.http.expectOne(`/api/jobs/${dispatcher.id}/required-skills`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      requiredSkills: [
        { skillId: routePlanning.id, expectedLevel: 'Advanced', criticality: 'High' },
        { skillId: forkliftDriving.id, expectedLevel: 'Expert', criticality: 'Low' },
      ],
    });
    request.flush(dispatcher);
    await app.settle();
    await flushJob();

    expect(app.text('[role="status"]')).toBe('The required skills have been saved.');
  });

  it('lets HR remove a required skill', async () => {
    await openAs('HrAdmin');

    await app.click(`${rows} button`);
    expect(app.element.querySelectorAll(rows)).toHaveLength(0);
    await app.submit(requiredForm);
    const request = app.http.expectOne(`/api/jobs/${dispatcher.id}/required-skills`);
    expect(request.request.body).toEqual({ requiredSkills: [] });
    request.flush({ ...dispatcher, requiredSkills: [] });
    await app.settle();
    await flushJob({ ...dispatcher, requiredSkills: [] });

    expect(app.text('tbody td')).toBe('No required skills yet.');
  });

  it('asks for a skill on every line', async () => {
    await openAs('HrAdmin');

    await app.click(`${requiredForm} button.button-secondary`);
    await app.submit(requiredForm);

    expect(app.texts(`${requiredForm} csh-field-errors p`)).toEqual([
      'Choose a skill on every line.',
    ]);
  });

  it('shows the API messages about required skills', async () => {
    await openAs('HrAdmin');

    await app.submit(requiredForm);
    app.http
      .expectOne(`/api/jobs/${dispatcher.id}/required-skills`)
      .flush(
        { errors: { 'requiredSkills[0].skillId': ['The skill does not exist.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();

    expect(app.texts(`${requiredForm} [role="alert"]`)).toEqual(['The skill does not exist.']);
  });

  it('shows why the API refused the required skills', async () => {
    await openAs('HrAdmin');

    await app.submit(requiredForm);
    app.http
      .expectOne(`/api/jobs/${dispatcher.id}/required-skills`)
      .flush(
        { title: 'Only HR admins can manage the organisation.' },
        { status: 403, statusText: 'Forbidden' },
      );
    await app.settle();

    expect(app.texts(`${requiredForm} [role="alert"]`)).toEqual([
      'Only HR admins can manage the organisation.',
    ]);
  });

  it('lets HR edit the job', async () => {
    await openAs('HrAdmin');
    expect(app.input('title').value).toBe('Dispatcher');
    expect(app.input('description').value).toBe('Plans the daily rounds.');

    await app.type('title', 'Senior dispatcher');
    await app.select('departmentId', 'department-people');
    await app.submit(jobForm);
    const request = app.http.expectOne({ method: 'PUT', url: `/api/jobs/${dispatcher.id}` });
    expect(request.request.body).toEqual({
      title: 'Senior dispatcher',
      departmentId: 'department-people',
      description: 'Plans the daily rounds.',
    });
    request.flush(dispatcher);
    await app.settle();
    await flushJob();

    expect(app.text('[role="status"]')).toBe('The job has been saved.');
  });

  it('sends an empty job description as none', async () => {
    await openAs('HrAdmin', warehouseLead);
    expect(app.input('description').value).toBe('');

    await app.submit(jobForm);
    const request = app.http.expectOne({ method: 'PUT', url: `/api/jobs/${warehouseLead.id}` });
    expect(request.request.body.description).toBeNull();
    request.flush({ title: 'This job already exists.' }, { status: 409, statusText: 'Conflict' });
    await app.settle();

    expect(app.text(`${jobForm} [role="alert"]`)).toBe('This job already exists.');
  });

  it('checks the job fields', async () => {
    await openAs('HrAdmin');

    await app.type('title', '');
    await app.submit(jobForm);

    expect(app.texts(`${jobForm} csh-field-errors p`)).toEqual(['Enter the title of the job.']);
  });

  it('lets HR delete the job', async () => {
    await openAs('HrAdmin');

    await app.click(`${jobForm} button.button-secondary`);
    app.http
      .expectOne({ method: 'DELETE', url: `/api/jobs/${dispatcher.id}` })
      .flush(null, { status: 204, statusText: 'No Content' });
    await app.settle();

    expect(app.router.url).toBe('/jobs');
    app.http.expectOne('/api/jobs').flush(jobs);
    app.http.expectOne('/api/departments').flush(departments);
  });

  it('tells why the job cannot be deleted', async () => {
    await openAs('HrAdmin');

    await app.click(`${jobForm} button.button-secondary`);
    app.http
      .expectOne(`/api/jobs/${dispatcher.id}`)
      .flush({ title: 'Employees still hold this job.' }, { status: 409, statusText: 'Conflict' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Employees still hold this job.');
  });
});

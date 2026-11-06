import { AppHarness } from '../../testing/app-harness';
import { forkliftDriving, routePlanning, skillDomains } from '../../testing/fixtures';

const domainForm = '[data-testid="domain-form"]';
const skillForm = '[data-testid="skill-form"]';
const domainButtons = '[data-testid="domain"] > div button';
const skillButtons = '[data-testid="skill"] button';

describe('SkillsPage', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openAs(role: 'HrAdmin' | 'Manager' | 'Employee'): Promise<void> {
    app.signIn(role);
    await app.open('/skills');
    expect(app.text('[role="status"]')).toBe('Loading the skills framework…');
    await flushFramework();
  }

  async function flushFramework(domains = skillDomains): Promise<void> {
    app.http.expectOne({ method: 'GET', url: '/api/skill-domains' }).flush(domains);
    await app.settle();
  }

  async function answer(method: string, url: string, body: object | null): Promise<unknown> {
    const request = app.http.expectOne({ method, url });
    request.flush(body);
    await app.settle();
    return request.request.body;
  }

  async function fillLevels(): Promise<void> {
    await app.type('beginner', 'Knows the basics.');
    await app.type('intermediate', 'Works alone.');
    await app.type('advanced', 'Handles hard cases.');
    await app.type('expert', 'Teaches others.');
  }

  it('needs a signed-in user', async () => {
    await app.open('/skills');

    expect(app.router.url).toBe('/login?returnUrl=%2Fskills');
  });

  it('shows the domains, their skills and the four levels', async () => {
    await openAs('Employee');

    expect(app.texts('[data-testid="domain"] h2')).toEqual(['Logistics', 'People skills']);
    expect(app.texts('[data-testid="domain"] > div p')).toEqual(['Moving goods.']);
    expect(app.texts('[data-testid="skill"] p.font-medium')).toEqual([
      'Forklift driving',
      'Route planning',
    ]);
    expect(app.texts('[data-testid="skill"] p.text-sm')).toEqual(['Plans delivery rounds.']);
    expect(app.texts('[data-testid="skill"]:last-child dd')).toEqual([
      'Follows a planned round.',
      'Plans a simple round.',
      'Plans rounds for a depot.',
      'Designs the planning rules.',
    ]);
    expect(app.texts('[data-testid="domain"]:last-of-type li')).toEqual([
      'No skills in this domain yet.',
    ]);
    expect(app.element.querySelector('form')).toBeNull();
    expect(app.element.querySelector('[data-testid="domain"] button')).toBeNull();
  });

  it('says when the framework is empty', async () => {
    app.signIn('Manager');
    await app.open('/skills');
    await flushFramework([]);

    expect(app.text('section > p:last-child')).toBe('No skill domains yet.');
  });

  it('tells when the framework cannot be loaded', async () => {
    app.signIn('Manager');
    await app.open('/skills');
    app.http.expectOne('/api/skill-domains').flush(null, { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The skills framework could not be loaded.');
  });

  it('lets HR add a domain', async () => {
    await openAs('HrAdmin');
    expect(app.text(`${domainForm} h2`)).toBe('Add a skill domain');

    await app.type('domainName', 'Safety');
    await app.type('domainDescription', 'Working safely.');
    await app.submit(domainForm);
    const body = await answer('POST', '/api/skill-domains', {});
    await flushFramework();

    expect(body).toEqual({ name: 'Safety', description: 'Working safely.' });
    expect(app.input('domainName').value).toBe('');
  });

  it('checks the domain name', async () => {
    await openAs('HrAdmin');

    await app.submit(domainForm);

    expect(app.texts(`${domainForm} csh-field-errors p`)).toEqual([
      'Enter the name of the domain.',
    ]);
  });

  it('shows why the API refused a domain', async () => {
    await openAs('HrAdmin');

    await app.type('domainName', 'Logistics');
    await app.submit(domainForm);
    app.http
      .expectOne('/api/skill-domains')
      .flush({ title: 'This domain already exists.' }, { status: 409, statusText: 'Conflict' });
    await app.settle();

    expect(app.text(`${domainForm} [role="alert"]`)).toBe('This domain already exists.');
  });

  it('lets HR edit a domain', async () => {
    await openAs('HrAdmin');

    await app.click(domainButtons);
    expect(app.text(`${domainForm} h2`)).toBe('Edit Logistics');
    expect(app.input('domainDescription').value).toBe('Moving goods.');
    await app.click(`${domainForm} button[type="button"]`);
    expect(app.text(`${domainForm} h2`)).toBe('Add a skill domain');

    const peopleEdit = app.element.querySelectorAll<HTMLButtonElement>(domainButtons)[2];
    peopleEdit.click();
    await app.settle();
    expect(app.input('domainDescription').value).toBe('');
    expect(app.text(`${domainForm} button[type="submit"]`)).toBe('Save');
    await app.submit(domainForm);
    const body = await answer('PUT', '/api/skill-domains/domain-people', {});
    await flushFramework();

    expect(body).toEqual({ name: 'People skills', description: null });
    expect(app.text(`${domainForm} h2`)).toBe('Add a skill domain');
  });

  it('lets HR delete a domain', async () => {
    await openAs('HrAdmin');

    await app.click(`[data-testid="domain"] > div button:last-child`);
    await answer('DELETE', '/api/skill-domains/domain-logistics', null);
    await flushFramework([skillDomains[1]]);

    expect(app.texts('[data-testid="domain"] h2')).toEqual(['People skills']);
  });

  it('tells why a domain cannot be deleted', async () => {
    await openAs('HrAdmin');

    await app.click(`[data-testid="domain"] > div button:last-child`);
    app.http
      .expectOne('/api/skill-domains/domain-logistics')
      .flush({ title: 'The domain still has skills.' }, { status: 409, statusText: 'Conflict' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The domain still has skills.');
  });

  it('lets HR add a skill with its four levels', async () => {
    await openAs('HrAdmin');
    expect(app.text(`${skillForm} h2`)).toBe('Add a skill');
    expect(app.texts('#skillDomainId option')).toEqual([
      'Choose a domain',
      'Logistics',
      'People skills',
    ]);

    await app.select('skillDomainId', 'domain-people');
    await app.type('skillName', 'Coaching');
    await fillLevels();
    await app.submit(skillForm);
    const body = await answer('POST', '/api/skills', {});
    await flushFramework();

    expect(body).toEqual({
      domainId: 'domain-people',
      name: 'Coaching',
      description: null,
      levels: {
        beginner: 'Knows the basics.',
        intermediate: 'Works alone.',
        advanced: 'Handles hard cases.',
        expert: 'Teaches others.',
      },
    });
    expect(app.input('skillName').value).toBe('');
  });

  it('checks every field of a skill', async () => {
    await openAs('HrAdmin');

    await app.submit(skillForm);

    expect(app.texts(`${skillForm} csh-field-errors p`)).toEqual([
      'Choose a domain.',
      'Enter the name of the skill.',
      'Describe the Beginner level.',
      'Describe the Intermediate level.',
      'Describe the Advanced level.',
      'Describe the Expert level.',
    ]);
  });

  it('shows the API messages for a skill', async () => {
    await openAs('HrAdmin');

    await app.select('skillDomainId', 'domain-people');
    await app.type('skillName', 'Coaching');
    await fillLevels();
    await app.submit(skillForm);
    app.http
      .expectOne('/api/skills')
      .flush(
        { errors: { 'levels.beginner': ['The level text is too long.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    await app.settle();

    expect(app.texts(`${skillForm} csh-field-errors p`)).toEqual(['The level text is too long.']);
  });

  it('shows why the API refused a skill', async () => {
    await openAs('HrAdmin');

    await app.select('skillDomainId', 'domain-people');
    await app.type('skillName', 'Coaching');
    await fillLevels();
    await app.submit(skillForm);
    app.http
      .expectOne('/api/skills')
      .flush({ title: 'This skill already exists.' }, { status: 409, statusText: 'Conflict' });
    await app.settle();

    expect(app.text(`${skillForm} [role="alert"]`)).toBe('This skill already exists.');
  });

  it('lets HR edit a skill', async () => {
    await openAs('HrAdmin');

    await app.click(skillButtons);
    expect(app.text(`${skillForm} h2`)).toBe(`Edit ${forkliftDriving.name}`);
    expect(app.input('skillDescription').value).toBe('');
    await app.click(`${skillForm} button[type="button"]`);
    expect(app.text(`${skillForm} h2`)).toBe('Add a skill');

    app.element.querySelectorAll<HTMLButtonElement>(skillButtons)[2].click();
    await app.settle();
    expect(app.input('expert').value).toBe(routePlanning.levels.expert);
    expect(app.text(`${skillForm} button[type="submit"]`)).toBe('Save');
    await app.type('skillName', 'Round planning');
    await app.submit(skillForm);
    const body = await answer('PUT', `/api/skills/${routePlanning.id}`, {});
    await flushFramework();

    expect(body).toEqual({
      domainId: routePlanning.domainId,
      name: 'Round planning',
      description: routePlanning.description,
      levels: routePlanning.levels,
    });
    expect(app.text(`${skillForm} h2`)).toBe('Add a skill');
  });

  it('lets HR delete a skill', async () => {
    await openAs('HrAdmin');

    app.element.querySelectorAll<HTMLButtonElement>(skillButtons)[1].click();
    await app.settle();
    await answer('DELETE', `/api/skills/${forkliftDriving.id}`, null);
    await flushFramework();

    expect(app.element.querySelector('[role="alert"]')).toBeNull();
  });
});

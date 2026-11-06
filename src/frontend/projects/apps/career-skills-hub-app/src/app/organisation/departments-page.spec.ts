import { AppHarness } from '../../testing/app-harness';
import { departments, operations } from '../../testing/fixtures';

describe('DepartmentsPage', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  async function openAs(role: 'HrAdmin' | 'Manager' | 'Employee'): Promise<void> {
    app.signIn(role);
    await app.open('/departments');
    expect(app.text('[role="status"]')).toBe('Loading the departments…');
    await flushList();
  }

  async function flushList(list = departments): Promise<void> {
    app.http.expectOne({ method: 'GET', url: '/api/departments' }).flush(list);
    await app.settle();
  }

  it('needs a signed-in user', async () => {
    await app.open('/departments');

    expect(app.router.url).toBe('/login?returnUrl=%2Fdepartments');
  });

  it('lets HR add a department', async () => {
    await openAs('HrAdmin');
    expect(app.texts('[data-testid="department"] span.font-medium')).toEqual([
      'Operations',
      'People',
    ]);
    expect(app.text('form h2')).toBe('Add a department');

    await app.type('name', 'Finance');
    await app.submit();
    const request = app.http.expectOne({ method: 'POST', url: '/api/departments' });
    expect(request.request.body).toEqual({ name: 'Finance' });
    request.flush({ id: 'department-finance', name: 'Finance' });
    await app.settle();
    await flushList([...departments, { id: 'department-finance', name: 'Finance' }]);

    expect(app.texts('[data-testid="department"]')).toHaveLength(3);
    expect(app.input('name').value).toBe('');
  });

  it('checks the name before sending it', async () => {
    await openAs('HrAdmin');

    await app.submit();

    expect(app.texts('csh-field-errors p')).toEqual(['Enter the name of the department.']);
  });

  it('shows why the API refused a department', async () => {
    await openAs('HrAdmin');

    await app.type('name', 'Operations');
    await app.submit();
    app.http
      .expectOne('/api/departments')
      .flush(
        { title: 'A department with this name already exists.' },
        { status: 409, statusText: 'Conflict' },
      );
    await app.settle();

    expect(app.text('form [role="alert"]')).toBe('A department with this name already exists.');
  });

  it('lets HR rename a department', async () => {
    await openAs('HrAdmin');

    await app.click('[data-testid="department"] button');
    expect(app.text('form h2')).toBe('Rename Operations');
    expect(app.input('name').value).toBe('Operations');
    await app.type('name', 'Operations and logistics');
    await app.submit();
    const request = app.http.expectOne(`/api/departments/${operations.id}`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Operations and logistics' });
    request.flush({ ...operations, name: 'Operations and logistics' });
    await app.settle();
    await flushList();

    expect(app.text('form h2')).toBe('Add a department');
  });

  it('lets HR cancel a rename', async () => {
    await openAs('HrAdmin');

    await app.click('[data-testid="department"] button');
    expect(app.text('form button[type="submit"]')).toBe('Save');
    await app.click('form button[type="button"]');

    expect(app.text('form h2')).toBe('Add a department');
    expect(app.input('name').value).toBe('');
    expect(app.element.querySelector('form button[type="button"]')).toBeNull();
  });

  it('lets HR delete a department', async () => {
    await openAs('HrAdmin');

    await app.click('[data-testid="department"] button:last-child');
    const request = app.http.expectOne(`/api/departments/${operations.id}`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await app.settle();
    await flushList([departments[1]]);

    expect(app.texts('[data-testid="department"] span.font-medium')).toEqual(['People']);
  });

  it('tells why a department cannot be deleted', async () => {
    await openAs('HrAdmin');

    await app.click('[data-testid="department"] button:last-child');
    app.http
      .expectOne(`/api/departments/${operations.id}`)
      .flush(
        { title: 'The department still has jobs or employees.' },
        { status: 409, statusText: 'Conflict' },
      );
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The department still has jobs or employees.');
  });

  it.each(['Manager', 'Employee'] as const)('shows the list read-only to a %s', async (role) => {
    await openAs(role);

    expect(app.texts('[data-testid="department"]')).toEqual(['Operations', 'People']);
    expect(app.element.querySelector('form')).toBeNull();
    expect(app.element.querySelector('[data-testid="department"] button')).toBeNull();
  });

  it('says when there are no departments', async () => {
    app.signIn('Employee');
    await app.open('/departments');
    await flushList([]);

    expect(app.text('ul li')).toBe('No departments yet.');
  });

  it('tells when the departments cannot be loaded', async () => {
    app.signIn('Employee');
    await app.open('/departments');
    app.http.expectOne('/api/departments').flush(null, { status: 500, statusText: 'Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('The departments could not be loaded.');
  });
});

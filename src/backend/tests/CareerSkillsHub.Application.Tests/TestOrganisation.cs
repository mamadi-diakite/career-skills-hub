namespace CareerSkillsHub.Application.Tests;

/// <summary>Organisation and employee data kept in lists, as both repositories share one unit of work.</summary>
internal sealed class InMemoryStore : IOrganisationRepository, IEmployeeRepository
{
    public List<Department> Departments { get; } = [];

    public List<Job> Jobs { get; } = [];

    public List<SkillDomain> SkillDomains { get; } = [];

    public List<Skill> Skills { get; } = [];

    public List<Employee> Employees { get; } = [];

    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<Department>> ListDepartmentsAsync(Guid companyId, CancellationToken cancellationToken) =>
        Of(Departments, d => d.CompanyId == companyId);

    public Task<IReadOnlyList<Job>> ListJobsAsync(Guid companyId, CancellationToken cancellationToken) =>
        Of(Jobs, j => j.CompanyId == companyId);

    public Task<IReadOnlyList<SkillDomain>> ListSkillDomainsAsync(Guid companyId, CancellationToken cancellationToken) =>
        Of(SkillDomains, d => d.CompanyId == companyId);

    public Task<IReadOnlyList<Skill>> ListSkillsAsync(Guid companyId, CancellationToken cancellationToken) =>
        Of(Skills, s => s.CompanyId == companyId);

    public Task<bool> IsDepartmentUsedAsync(Guid departmentId, CancellationToken cancellationToken) =>
        Task.FromResult(Jobs.Any(j => j.DepartmentId == departmentId)
            || Employees.Any(e => e.JobHistory.Any(a => a.DepartmentId == departmentId)));

    public Task<bool> IsJobUsedAsync(Guid jobId, CancellationToken cancellationToken) =>
        Task.FromResult(Employees.Any(e => e.JobHistory.Any(a => a.JobId == jobId)));

    public Task<IReadOnlyList<Employee>> ListAsync(Guid companyId, CancellationToken cancellationToken) =>
        Of(Employees, e => e.CompanyId == companyId);

    public Task<Employee?> FindAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(Employees.SingleOrDefault(e => e.CompanyId == companyId && e.Id == employeeId));

    public void Add(Department department) => Departments.Add(department);

    public void Add(Job job) => Jobs.Add(job);

    public void Add(SkillDomain domain) => SkillDomains.Add(domain);

    public void Add(Skill skill) => Skills.Add(skill);

    public void Add(Employee employee) => Employees.Add(employee);

    public void Remove(Department department) => Departments.Remove(department);

    public void Remove(Job job) => Jobs.Remove(job);

    public void Remove(SkillDomain domain) => SkillDomains.Remove(domain);

    public void Remove(Skill skill) => Skills.Remove(skill);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private static Task<IReadOnlyList<T>> Of<T>(List<T> items, Func<T, bool> predicate) =>
        Task.FromResult<IReadOnlyList<T>>([.. items.Where(predicate)]);
}

/// <summary>
/// Blue Harbour Logistics: HR admin Claire (no employee record), manager Marc with Ines in his team,
/// Nadia outside his team; plus another company that must stay invisible.
/// </summary>
internal sealed class TestOrganisation
{
    public static readonly SkillLevelDescriptions Levels = new("Knows the basics", "Works alone", "Handles hard cases", "Teaches others");

    public TestOrganisation()
    {
        var now = TestClock.Now.UtcDateTime;
        Company = new Company("Blue Harbour Logistics", 64, now);
        Users.AddCompany(Company);

        Warehouse = AddDepartment(Company, "Warehouse");
        Transport = AddDepartment(Company, "Transport");
        Unused = AddDepartment(Company, "Quality");

        Safety = AddDomain(Company, "Safety");
        Logistics = AddDomain(Company, "Logistics");
        Forklift = AddSkill(Company, Safety, "Forklift driving");
        SafetyRules = AddSkill(Company, Safety, "Workplace safety rules");
        RoutePlanning = AddSkill(Company, Logistics, "Route planning");

        ForkliftOperator = AddJob(Company, Warehouse, "Forklift operator");
        ForkliftOperator.SetRequiredSkills(
        [
            new RequiredSkill(SafetyRules.Id, SkillLevel.Intermediate, Criticality.High),
            new RequiredSkill(Forklift.Id, SkillLevel.Advanced, Criticality.High),
            new RequiredSkill(RoutePlanning.Id, SkillLevel.Beginner, Criticality.Low),
        ]);
        TeamLead = AddJob(Company, Warehouse, "Warehouse team lead");
        TruckDriver = AddJob(Company, Transport, "Truck driver");
        Vacant = AddJob(Company, Transport, "Dispatcher");

        Marc = AddEmployee(Company, "Marc", "Lefebvre", TeamLead, Warehouse, null, new DateOnly(2018, 3, 5));
        Ines = AddEmployee(Company, "Ines", "Moreau", ForkliftOperator, Warehouse, Marc.Id, new DateOnly(2022, 9, 1));
        Nadia = AddEmployee(Company, "Nadia", "Haddad", TruckDriver, Transport, null, new DateOnly(2019, 4, 15));

        Hr = AddUser(Company, "claire.fontaine@example.com", "Claire Fontaine", UserRole.HrAdmin, null);
        Manager = AddUser(Company, "marc.lefebvre@example.com", "Marc Lefebvre", UserRole.Manager, Marc.Id);
        Employee = AddUser(Company, "ines.moreau@example.com", "Ines Moreau", UserRole.Employee, Ines.Id);

        OtherCompany = new Company("Green Valley Foods", 20, now);
        Users.AddCompany(OtherCompany);
        OtherDepartment = AddDepartment(OtherCompany, "Kitchen");
        OtherDomain = AddDomain(OtherCompany, "Cooking");
        OtherSkill = AddSkill(OtherCompany, OtherDomain, "Food hygiene");
        OtherJob = AddJob(OtherCompany, OtherDepartment, "Cook");
        OtherEmployee = AddEmployee(OtherCompany, "Jade", "Roux", OtherJob, OtherDepartment, null, new DateOnly(2021, 2, 1));
        OtherHr = AddUser(OtherCompany, "hr@green.example.com", "Hugo Lambert", UserRole.HrAdmin, null);
    }

    public InMemoryUserRepository Users { get; } = new();

    public InMemoryStore Store { get; } = new();

    public FakePasswordHasher PasswordHasher { get; } = new();

    public ActorResolver Actors => new(Users);

    public Company Company { get; }

    public Department Warehouse { get; }

    public Department Transport { get; }

    /// <summary>No job and no employee.</summary>
    public Department Unused { get; }

    public SkillDomain Safety { get; }

    public SkillDomain Logistics { get; }

    public Skill Forklift { get; }

    public Skill SafetyRules { get; }

    public Skill RoutePlanning { get; }

    public Job ForkliftOperator { get; }

    public Job TeamLead { get; }

    public Job TruckDriver { get; }

    /// <summary>Held by nobody.</summary>
    public Job Vacant { get; }

    public Employee Marc { get; }

    public Employee Ines { get; }

    public Employee Nadia { get; }

    public User Hr { get; }

    public User Manager { get; }

    public User Employee { get; }

    public Company OtherCompany { get; }

    public Department OtherDepartment { get; }

    public SkillDomain OtherDomain { get; }

    public Skill OtherSkill { get; }

    public Job OtherJob { get; }

    public Employee OtherEmployee { get; }

    public User OtherHr { get; }

    /// <summary>Manager and employee: the two roles that cannot change the organisation.</summary>
    public IEnumerable<User> NonHrUsers => [Manager, Employee];

    private Department AddDepartment(Company company, string name)
    {
        var department = new Department(company.Id, name);
        Store.Departments.Add(department);
        return department;
    }

    private SkillDomain AddDomain(Company company, string name)
    {
        var domain = new SkillDomain(company.Id, name, null);
        Store.SkillDomains.Add(domain);
        return domain;
    }

    private Skill AddSkill(Company company, SkillDomain domain, string name)
    {
        var skill = new Skill(company.Id, domain.Id, name, null, Levels);
        Store.Skills.Add(skill);
        return skill;
    }

    private Job AddJob(Company company, Department department, string title)
    {
        var job = new Job(company.Id, department.Id, title, null);
        Store.Jobs.Add(job);
        return job;
    }

    private Employee AddEmployee(Company company, string firstName, string lastName, Job job, Department department, Guid? managerId, DateOnly hireDate)
    {
        var employee = new Employee(
            company.Id,
            firstName,
            lastName,
            $"{firstName}.{lastName}@example.com".ToLowerInvariant(),
            hireDate,
            job.Id,
            department.Id,
            managerId,
            TestClock.Now.UtcDateTime);
        Store.Employees.Add(employee);
        return employee;
    }

    private User AddUser(Company company, string email, string displayName, UserRole role, Guid? employeeId)
    {
        var user = new User(company.Id, email, displayName, "hashed:" + TestClock.Password, role, employeeId, TestClock.Now.UtcDateTime);
        Users.Add(user);
        return user;
    }
}
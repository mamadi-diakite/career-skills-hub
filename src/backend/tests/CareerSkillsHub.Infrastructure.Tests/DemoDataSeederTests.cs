namespace CareerSkillsHub.Infrastructure.Tests;

public sealed class DemoDataSeederTests : IDisposable
{
    private static readonly DateTime Now = TestOrganisation.Now;

    private readonly SqliteTestDatabase _database = new();
    private readonly IdentityPasswordHasher _hasher = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seeds_one_company_with_its_organisation_and_skills_framework()
    {
        Assert.True(await SeedAsync());

        await using var db = _database.CreateContext();
        var company = await db.Companies.SingleAsync();
        Assert.Equal((DemoCompany.Name, DemoCompany.Headcount), (company.Name, company.Headcount));
        Assert.Equal(DemoCompany.Departments.Order(), (await db.Departments.Select(d => d.Name).ToListAsync()).Order());
        Assert.Equal(DemoCompany.SkillDomains.Count, await db.SkillDomains.CountAsync());
        Assert.Equal(DemoCompany.SkillDomains.Sum(d => d.Skills.Count), await db.Skills.CountAsync());
        var jobs = await db.Jobs.Include(j => j.RequiredSkills).ToListAsync();
        Assert.Equal(DemoCompany.Jobs.Count, jobs.Count);
        Assert.All(jobs, j => Assert.NotEmpty(j.RequiredSkills));
        Assert.Contains(jobs.SelectMany(j => j.RequiredSkills), s => s.Criticality == Criticality.High);
    }

    [Fact]
    public async Task Seeds_employees_with_managers_and_job_history()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        var employees = await db.Employees.Include(e => e.JobHistory).ToDictionaryAsync(e => e.Email);
        Assert.Equal(DemoCompany.Employees.Count, employees.Count);
        Assert.All(employees.Keys, email => Assert.EndsWith("@example.com", email));

        var marc = employees["marc.lefebvre@example.com"];
        Assert.Equal(
            ["ines.moreau@example.com", "lucas.bernard@example.com", "tom.girard@example.com"],
            employees.Values.Where(e => e.ManagerId == marc.Id).Select(e => e.Email).Order());
        Assert.Equal(2, marc.JobHistory.Count);
        Assert.Equal(new DateOnly(2020, 1, 5), marc.JobHistory[0].EndDate);
        Assert.Single(employees["ines.moreau@example.com"].JobHistory);
    }

    [Fact]
    public async Task Seeds_one_account_per_role_linked_to_its_employee_record()
    {
        await SeedAsync();

        await using var db = _database.CreateContext();
        var users = await db.Users.ToListAsync();
        var employees = await db.Employees.ToDictionaryAsync(e => e.Id, e => e.Email);

        Assert.Equal(
            [(UserRole.HrAdmin, "claire.fontaine@example.com"), (UserRole.Manager, "marc.lefebvre@example.com"),
                (UserRole.Employee, "ines.moreau@example.com")],
            users.OrderBy(u => u.Role).Select(u => (u.Role, u.Email)));
        Assert.All(users, u => Assert.Equal(u.Email, employees[u.EmployeeId!.Value]));
        Assert.All(users, u => Assert.True(_hasher.Verify(u.PasswordHash, DemoDataSeeder.DemoPassword), u.Email));
        Assert.All(users, u => Assert.Equal(Now, u.CreatedAtUtc));
    }

    [Fact]
    public async Task Second_run_adds_nothing()
    {
        await SeedAsync();

        Assert.False(await SeedAsync());

        await using var db = _database.CreateContext();
        Assert.Equal(1, await db.Companies.CountAsync());
        Assert.Equal(DemoCompany.Employees.Count, await db.Employees.CountAsync());
    }

    [Fact]
    public async Task Skips_a_database_that_already_has_users()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        await using (var db = _database.CreateContext())
        {
            db.Users.Add(new User(org.Company.Id, "someone@example.com", "Someone", "hash", UserRole.HrAdmin, null, Now));
            await db.SaveChangesAsync();
        }

        Assert.False(await SeedAsync());

        await using var check = _database.CreateContext();
        Assert.Equal(1, await check.Companies.CountAsync());
        Assert.Equal(0, await check.Employees.CountAsync());
    }

    private async Task<bool> SeedAsync()
    {
        await using var db = _database.CreateContext();

        return await new DemoDataSeeder(db, _hasher, new FixedTimeProvider(Now)).SeedAsync();
    }
}

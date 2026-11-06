namespace CareerSkillsHub.Infrastructure.Tests;

public sealed class CareerSkillsHubDbContextTests : IDisposable
{
    private static readonly DateTime Now = TestOrganisation.Now;

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_the_organisation()
    {
        var org = await TestOrganisation.CreateAsync(_database);

        await using var db = _database.CreateContext();
        var company = await db.Companies.SingleAsync();
        Assert.Equal(("Blue Harbour Logistics", 64), (company.Name, company.Headcount));
        Assert.Equal(DateTimeKind.Utc, company.CreatedAtUtc.Kind);
        Assert.Equal(org.Department.Id, (await db.Jobs.SingleAsync()).DepartmentId);
        Assert.Equal("Warehouse", (await db.Departments.SingleAsync()).Name);
        Assert.Equal("Safety", (await db.SkillDomains.SingleAsync()).Name);
        var skill = await db.Skills.SingleAsync();
        Assert.Equal(org.Domain.Id, skill.DomainId);
        Assert.Equal(
            TestOrganisation.Levels,
            new SkillLevelDescriptions(skill.BeginnerDescription, skill.IntermediateDescription, skill.AdvancedDescription, skill.ExpertDescription));
    }

    [Fact]
    public async Task Saves_required_skills_and_their_changes()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        await using (var db = _database.CreateContext())
        {
            var job = await db.Jobs.SingleAsync();
            job.SetRequiredSkills([new(org.Skill.Id, SkillLevel.Advanced, Criticality.High)]);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var job = await db.Jobs.Include(j => j.RequiredSkills).SingleAsync();
            var required = Assert.Single(job.RequiredSkills);
            Assert.Equal((SkillLevel.Advanced, Criticality.High), (required.ExpectedLevel, required.Criticality));

            job.SetRequiredSkills([new(org.Skill.Id, SkillLevel.Expert, Criticality.Medium)]);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var job = await db.Jobs.Include(j => j.RequiredSkills).SingleAsync();
            Assert.Equal(SkillLevel.Expert, Assert.Single(job.RequiredSkills).ExpectedLevel);

            job.SetRequiredSkills([]);
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        Assert.Empty(await check.Set<JobRequiredSkill>().ToListAsync());
    }

    [Fact]
    public async Task Saves_employees_with_their_manager_and_job_history()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        var otherJob = new Job(org.Company.Id, org.Department.Id, "Warehouse team lead", null);
        var manager = org.NewEmployee("Marc", "marc.martin@example.com");
        var employee = org.NewEmployee("Hugo", "hugo.martin@example.com", manager.Id);
        await using (var db = _database.CreateContext())
        {
            db.AddRange(otherJob, manager, employee);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Employees.Include(e => e.JobHistory).SingleAsync(e => e.Id == employee.Id);
            loaded.ChangeJob(otherJob.Id, org.Department.Id, new DateOnly(2025, 6, 1));
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        var saved = await check.Employees.Include(e => e.JobHistory).SingleAsync(e => e.Id == employee.Id);
        Assert.Equal(manager.Id, saved.ManagerId);
        Assert.Equal(new DateOnly(2022, 5, 2), saved.HireDate);
        Assert.Equal(otherJob.Id, saved.JobId);
        Assert.Equal(
            [(org.Job.Id, (DateOnly?)new DateOnly(2025, 5, 31)), (otherJob.Id, null)],
            saved.JobHistory.Select(a => (a.JobId, a.EndDate)));
    }

    [Fact]
    public async Task Saves_users_linked_to_their_employee_record()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        var employee = org.NewEmployee("Hugo", "hugo.martin@example.com");
        var user = new User(org.Company.Id, employee.Email, employee.FullName, "hash", UserRole.Employee, employee.Id, Now);
        await using (var db = _database.CreateContext())
        {
            db.AddRange(employee, user);
            await db.SaveChangesAsync();
        }

        await using var check = _database.CreateContext();
        var saved = await check.Users.SingleAsync();
        Assert.Equal((UserRole.Employee, employee.Id), (saved.Role, saved.EmployeeId));
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task User_email_is_unique_across_companies()
    {
        var first = await TestOrganisation.CreateAsync(_database);
        var second = await TestOrganisation.CreateAsync(_database, "Green Valley Foods");

        await using var db = _database.CreateContext();
        db.Users.Add(new User(first.Company.Id, "hr@example.com", "HR", "hash", UserRole.HrAdmin, null, Now));
        db.Users.Add(new User(second.Company.Id, "HR@Example.com", "HR 2", "hash", UserRole.HrAdmin, null, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Names_are_unique_within_a_company_only()
    {
        var first = await TestOrganisation.CreateAsync(_database);
        var second = await TestOrganisation.CreateAsync(_database, "Green Valley Foods");

        await using var db = _database.CreateContext();
        Assert.Equal("Warehouse", (await db.Departments.SingleAsync(d => d.CompanyId == second.Company.Id)).Name);

        db.Departments.Add(new Department(first.Company.Id, "Warehouse"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Employee_email_is_unique_within_a_company()
    {
        var org = await TestOrganisation.CreateAsync(_database);

        await using var db = _database.CreateContext();
        db.Employees.Add(org.NewEmployee("Hugo", "hugo.martin@example.com"));
        db.Employees.Add(org.NewEmployee("Hugo", "Hugo.Martin@example.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_department_with_jobs_cannot_be_deleted()
    {
        await TestOrganisation.CreateAsync(_database);

        await using var db = _database.CreateContext();
        db.Departments.Remove(await db.Departments.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_required_skill_cannot_be_deleted()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        await using (var db = _database.CreateContext())
        {
            (await db.Jobs.SingleAsync()).SetRequiredSkills([new(org.Skill.Id, SkillLevel.Beginner, Criticality.Low)]);
            await db.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        context.Skills.Remove(await context.Skills.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}

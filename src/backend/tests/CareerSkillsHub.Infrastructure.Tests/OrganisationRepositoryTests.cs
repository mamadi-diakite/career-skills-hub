namespace CareerSkillsHub.Infrastructure.Tests;

public sealed class OrganisationRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Lists_only_the_data_of_the_company_with_required_skills()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        await TestOrganisation.CreateAsync(_database, "Green Valley Foods");
        await using (var setup = _database.CreateContext())
        {
            var job = await setup.Jobs.SingleAsync(j => j.Id == org.Job.Id);
            job.SetRequiredSkills([new RequiredSkill(org.Skill.Id, SkillLevel.Advanced, Criticality.High)]);
            await setup.SaveChangesAsync();
        }

        await using var db = _database.CreateContext();
        var repository = new OrganisationRepository(db);

        Assert.Equal([org.Department.Id], (await repository.ListDepartmentsAsync(org.Company.Id, default)).Select(d => d.Id));
        Assert.Equal([org.Domain.Id], (await repository.ListSkillDomainsAsync(org.Company.Id, default)).Select(d => d.Id));
        Assert.Equal([org.Skill.Id], (await repository.ListSkillsAsync(org.Company.Id, default)).Select(s => s.Id));
        var listed = Assert.Single(await repository.ListJobsAsync(org.Company.Id, default));
        Assert.Equal((org.Skill.Id, SkillLevel.Advanced), (listed.RequiredSkills.Single().SkillId, listed.RequiredSkills.Single().ExpectedLevel));
    }

    [Fact]
    public async Task Adds_updates_and_removes_through_the_unit_of_work()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        var department = new Department(org.Company.Id, "Transport");
        var domain = new SkillDomain(org.Company.Id, "Logistics", null);
        var skill = new Skill(org.Company.Id, domain.Id, "Route planning", null, TestOrganisation.Levels);
        var job = new Job(org.Company.Id, department.Id, "Truck driver", null);
        await using (var db = _database.CreateContext())
        {
            var repository = new OrganisationRepository(db);
            repository.Add(department);
            repository.Add(domain);
            repository.Add(skill);
            repository.Add(job);
            (await repository.ListSkillsAsync(org.Company.Id, default)).Single(s => s.Id == org.Skill.Id)
                .Update(org.Domain.Id, "Reach truck driving", null, TestOrganisation.Levels);
            await repository.SaveChangesAsync(default);
        }

        await using (var db = _database.CreateContext())
        {
            Assert.Equal(2, await db.Jobs.CountAsync());
            Assert.Equal("Reach truck driving", (await db.Skills.SingleAsync(s => s.Id == org.Skill.Id)).Name);

            var repository = new OrganisationRepository(db);
            repository.Remove(await db.Jobs.SingleAsync(j => j.Id == job.Id));
            repository.Remove(await db.Skills.SingleAsync(s => s.Id == skill.Id));
            repository.Remove(await db.SkillDomains.SingleAsync(d => d.Id == domain.Id));
            repository.Remove(await db.Departments.SingleAsync(d => d.Id == department.Id));
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(
            (1, 1, 1, 1),
            (await check.Departments.CountAsync(), await check.Jobs.CountAsync(), await check.SkillDomains.CountAsync(), await check.Skills.CountAsync()));
    }

    [Fact]
    public async Task Department_is_used_by_a_job_or_by_the_job_history()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        var formerDepartment = new Department(org.Company.Id, "Transport");
        var emptyDepartment = new Department(org.Company.Id, "Quality");
        var employee = new Employee(
            org.Company.Id, "Tom", "Girard", "tom.girard@example.com", new DateOnly(2019, 1, 7), org.Job.Id, formerDepartment.Id, null, TestOrganisation.Now);
        employee.ChangeJob(org.Job.Id, org.Department.Id, new DateOnly(2021, 1, 4));
        await using (var setup = _database.CreateContext())
        {
            setup.AddRange(formerDepartment, emptyDepartment, employee);
            await setup.SaveChangesAsync();
        }

        await using var db = _database.CreateContext();
        var repository = new OrganisationRepository(db);

        Assert.True(await repository.IsDepartmentUsedAsync(org.Department.Id, default));
        Assert.True(await repository.IsDepartmentUsedAsync(formerDepartment.Id, default));
        Assert.False(await repository.IsDepartmentUsedAsync(emptyDepartment.Id, default));
    }

    [Fact]
    public async Task Job_is_used_when_someone_holds_or_held_it()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        var vacant = new Job(org.Company.Id, org.Department.Id, "Dispatcher", null);
        var employee = org.NewEmployee("Lucas", "lucas.martin@example.com");
        await using (var setup = _database.CreateContext())
        {
            setup.AddRange(vacant, employee);
            await setup.SaveChangesAsync();
        }

        await using var db = _database.CreateContext();
        var repository = new OrganisationRepository(db);

        Assert.True(await repository.IsJobUsedAsync(org.Job.Id, default));
        Assert.False(await repository.IsJobUsedAsync(vacant.Id, default));
    }
}

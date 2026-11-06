namespace CareerSkillsHub.Infrastructure.Tests;

public sealed class EmployeeRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Lists_and_finds_only_employees_of_the_company()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        var other = await TestOrganisation.CreateAsync(_database, "Green Valley Foods");
        var lucas = org.NewEmployee("Lucas", "lucas.martin@example.com");
        var jade = other.NewEmployee("Jade", "jade.martin@example.com");
        await using (var setup = _database.CreateContext())
        {
            setup.AddRange(lucas, jade);
            await setup.SaveChangesAsync();
        }

        await using var db = _database.CreateContext();
        var repository = new EmployeeRepository(db);

        Assert.Equal([lucas.Id], (await repository.ListAsync(org.Company.Id, default)).Select(e => e.Id));
        Assert.Single((await repository.FindAsync(org.Company.Id, lucas.Id, default))!.JobHistory);
        Assert.Null(await repository.FindAsync(org.Company.Id, jade.Id, default));
    }

    [Fact]
    public async Task Adds_an_employee_and_saves_a_job_change()
    {
        var org = await TestOrganisation.CreateAsync(_database);
        var lead = new Job(org.Company.Id, org.Department.Id, "Warehouse team lead", null);
        var lucas = org.NewEmployee("Lucas", "lucas.martin@example.com");
        await using (var db = _database.CreateContext())
        {
            db.Jobs.Add(lead);
            var repository = new EmployeeRepository(db);
            repository.Add(lucas);
            await repository.SaveChangesAsync(default);
        }

        await using (var db = _database.CreateContext())
        {
            var repository = new EmployeeRepository(db);
            (await repository.FindAsync(org.Company.Id, lucas.Id, default))!.ChangeJob(lead.Id, org.Department.Id, new DateOnly(2025, 1, 6));
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var history = (await new EmployeeRepository(check).FindAsync(org.Company.Id, lucas.Id, default))!.JobHistory;
        Assert.Equal([org.Job.Id, lead.Id], history.Select(a => a.JobId));
        Assert.Equal(new DateOnly(2025, 1, 5), history[0].EndDate);
    }
}

namespace CareerSkillsHub.Infrastructure.Tests;

public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTime Now = TestOrganisation.Now;

    private readonly SqliteTestDatabase _database = new();
    private readonly Company _company = new("Blue Harbour Logistics", 64, Now);
    private readonly User _user;

    public UserRepositoryTests()
    {
        _user = new User(_company.Id, "claire.fontaine@example.com", "Claire", "hash", UserRole.HrAdmin, null, Now);
        using var db = _database.CreateContext();
        db.Companies.Add(_company);
        db.Users.Add(_user);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Finds_a_user_by_email_and_by_id_and_its_company()
    {
        await using var db = _database.CreateContext();
        var repository = new UserRepository(db);

        Assert.Equal(_user.Id, (await repository.FindByEmailAsync("claire.fontaine@example.com", default))?.Id);
        Assert.Equal(_user.Email, (await repository.FindByIdAsync(_user.Id, default))?.Email);
        Assert.Equal("Blue Harbour Logistics", (await repository.FindCompanyAsync(_company.Id, default))?.Name);
        Assert.Null(await repository.FindByEmailAsync("nobody@example.com", default));
        Assert.Null(await repository.FindByIdAsync(Guid.NewGuid(), default));
        Assert.Null(await repository.FindCompanyAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Lists_the_accounts_of_one_company()
    {
        var other = new Company("Green Valley Foods", 12, Now);
        await using (var setup = _database.CreateContext())
        {
            setup.Companies.Add(other);
            setup.Users.Add(new User(other.Id, "jade.roux@example.com", "Jade", "hash", UserRole.HrAdmin, null, Now));
            await setup.SaveChangesAsync();
        }

        await using var db = _database.CreateContext();

        Assert.Equal([_user.Id], (await new UserRepository(db).ListByCompanyAsync(_company.Id, default)).Select(u => u.Id));
    }

    [Fact]
    public async Task Adds_a_company_with_its_hr_admin_and_saves_a_changed_password()
    {
        var company = new Company("Green Valley Foods", 12, Now);
        var hr = new User(company.Id, "jade.roux@example.com", "Jade", "hash", UserRole.HrAdmin, null, Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new UserRepository(db);
            repository.AddCompany(company);
            repository.Add(hr);
            (await repository.FindByIdAsync(_user.Id, default))!.ChangePassword("new-hash");
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(company.Id, (await check.Users.SingleAsync(u => u.Id == hr.Id)).CompanyId);
        Assert.Equal("new-hash", (await new UserRepository(check).FindByIdAsync(_user.Id, default))?.PasswordHash);
    }
}

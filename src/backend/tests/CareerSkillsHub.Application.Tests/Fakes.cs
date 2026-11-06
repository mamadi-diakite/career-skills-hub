namespace CareerSkillsHub.Application.Tests;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public List<Company> Companies { get; } = [];

    public int SaveCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

    public Task<Company?> FindCompanyAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Companies.SingleOrDefault(c => c.Id == id));

    public void Add(User user) => Users.Add(user);

    public void AddCompany(Company company) => Companies.Add(company);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    /// <summary>Adds a company with one account of each role.</summary>
    public TestCompany AddTestCompany(string name = "Blue Harbour Logistics")
    {
        var company = new Company(name, 64, TestClock.Now.UtcDateTime);
        var slug = name.Split(' ')[0].ToLowerInvariant();
        var hr = new User(company.Id, $"hr@{slug}.example.com", "Claire Fontaine", "hashed:" + TestClock.Password,
            UserRole.HrAdmin, null, TestClock.Now.UtcDateTime);
        var manager = new User(company.Id, $"manager@{slug}.example.com", "Marc Lefebvre", "hashed:" + TestClock.Password,
            UserRole.Manager, Guid.NewGuid(), TestClock.Now.UtcDateTime);
        var employee = new User(company.Id, $"employee@{slug}.example.com", "Ines Moreau", "hashed:" + TestClock.Password,
            UserRole.Employee, Guid.NewGuid(), TestClock.Now.UtcDateTime);
        Companies.Add(company);
        Users.AddRange(hr, manager, employee);

        return new TestCompany(company, hr, manager, employee);
    }
}

internal sealed record TestCompany(Company Company, User Hr, User Manager, User Employee);

/// <summary>Readable fake: the hash is the password with a prefix.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCount { get; private set; }

    public string Hash(string password)
    {
        HashCount++;
        return "hashed:" + password;
    }

    public bool Verify(string passwordHash, string password) => passwordHash == "hashed:" + password;
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public static readonly DateTime ExpiresAt = new(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

    public AccessToken Issue(User user) => new($"token-for-{user.Id}", ExpiresAt);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal static class TestClock
{
    public const string Password = "CareerDemo123!";

    public static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
}

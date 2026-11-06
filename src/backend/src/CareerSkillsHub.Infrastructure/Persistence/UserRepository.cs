namespace CareerSkillsHub.Infrastructure.Persistence;

public sealed class UserRepository(CareerSkillsHubDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Company?> FindCompanyAsync(Guid id, CancellationToken cancellationToken) =>
        db.Companies.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public void AddCompany(Company company) => db.Companies.Add(company);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

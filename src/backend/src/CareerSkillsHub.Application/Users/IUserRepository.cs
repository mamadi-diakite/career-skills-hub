namespace CareerSkillsHub.Application.Users;

public interface IUserRepository
{
    /// <param name="email">Normalized email: trimmed and lowercase.</param>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Company?> FindCompanyAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);

    void AddCompany(Company company);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

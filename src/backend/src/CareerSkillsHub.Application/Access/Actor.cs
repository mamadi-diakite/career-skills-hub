namespace CareerSkillsHub.Application.Access;

/// <summary>The signed-in user, read from the database on every request.</summary>
/// <param name="EmployeeId">Employee record of the user; always set for managers and employees.</param>
public sealed record Actor(Guid UserId, Guid CompanyId, UserRole Role, Guid? EmployeeId)
{
    public static Actor From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new Actor(user.Id, user.CompanyId, user.Role, user.EmployeeId);
    }
}

public sealed class ActorResolver(IUserRepository users)
{
    /// <returns><c>null</c> when the account no longer exists.</returns>
    public async Task<Actor?> FindAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await users.FindByIdAsync(userId, cancellationToken) is { } user ? Actor.From(user) : null;
}

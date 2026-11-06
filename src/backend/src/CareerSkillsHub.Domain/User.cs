namespace CareerSkillsHub.Domain;

/// <summary>A login account in a company.</summary>
public sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;
    public const int PasswordHashMaxLength = 500;

    private User()
    {
    }

    public User(
        Guid companyId,
        string email,
        string displayName,
        string passwordHash,
        UserRole role,
        Guid? employeeId,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        CompanyId = Guard.NotEmpty(companyId, nameof(companyId));
        Email = Guard.Email(email, nameof(email), EmailMaxLength);
        DisplayName = Guard.Required(displayName, nameof(displayName), DisplayNameMaxLength);
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), PasswordHashMaxLength);
        Role = Guard.Defined(role, nameof(role));
        EmployeeId = Guard.NotEmpty(employeeId, nameof(employeeId));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));

        if (Role != UserRole.HrAdmin && EmployeeId is null)
        {
            throw new ArgumentException("Managers and employees are linked to their employee record.", nameof(employeeId));
        }
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    /// <summary>Employee record of the account holder. Optional for HR admins.</summary>
    public Guid? EmployeeId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}

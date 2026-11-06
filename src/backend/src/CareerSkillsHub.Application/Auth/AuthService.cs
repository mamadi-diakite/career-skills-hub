namespace CareerSkillsHub.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer,
    TimeProvider timeProvider)
{
    /// <summary>Creates a company and its first HR admin, then opens a session for them.</summary>
    public async Task<SignUpResult> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new FieldErrors();
        var companyName = errors.Required("companyName", request.CompanyName, Company.NameMaxLength, "Company name");
        errors.Range("headcount", request.Headcount, 1, Company.MaxHeadcount, "Headcount");
        var displayName = errors.Required("displayName", request.DisplayName, User.DisplayNameMaxLength, "Name");

        string? email = null;
        if (!User.IsValidEmail(request.Email))
        {
            errors["email"] = ["Enter a valid email address."];
        }
        else
        {
            email = request.Email.Trim().ToLowerInvariant();
            if (await users.FindByEmailAsync(email, cancellationToken) is not null)
            {
                errors["email"] = ["An account already uses this email."];
            }
        }

        AddNewPasswordErrors(errors, "password", request.Password, request.ConfirmPassword);

        if (errors.Count > 0)
        {
            return SignUpResult.Invalid(errors);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var company = new Company(companyName!, request.Headcount, now);
        var user = new User(company.Id, email!, displayName!, passwordHasher.Hash(request.Password), UserRole.HrAdmin, null, now);
        users.AddCompany(company);
        users.Add(user);
        await users.SaveChangesAsync(cancellationToken);

        return SignUpResult.SignedUp(CreateSession(user, company));
    }

    /// <returns><c>null</c> when the email or the password is wrong, without telling which.</returns>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            // Hash anyway, so an unknown email takes as long as a wrong password.
            passwordHasher.Hash(request.Password);
            return null;
        }

        return passwordHasher.Verify(user.PasswordHash, request.Password)
            ? CreateSession(user, await CompanyOfAsync(user, cancellationToken))
            : null;
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);

        return user is null ? null : ToResponse(user, await CompanyOfAsync(user, cancellationToken));
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ChangePasswordResult.UserNotFound;
        }

        var errors = new FieldErrors();
        if (string.IsNullOrEmpty(request.CurrentPassword) || !passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            errors["currentPassword"] = ["Current password is not correct."];
        }

        AddNewPasswordErrors(errors, "newPassword", request.NewPassword, request.ConfirmPassword);
        if (!errors.ContainsKey("newPassword") && request.NewPassword == request.CurrentPassword)
        {
            errors["newPassword"] = ["New password must be different from the current one."];
        }

        if (errors.Count > 0)
        {
            return ChangePasswordResult.Invalid(errors);
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await users.SaveChangesAsync(cancellationToken);

        return ChangePasswordResult.Changed;
    }

    private static void AddNewPasswordErrors(FieldErrors errors, string passwordField, string password, string confirmPassword)
    {
        var policyErrors = PasswordPolicy.Check(password);
        if (policyErrors.Count > 0)
        {
            errors[passwordField] = [.. policyErrors];
        }

        if (confirmPassword != password)
        {
            errors["confirmPassword"] = ["Confirmation does not match the password."];
        }
    }

    // A user cannot exist without its company (foreign key).
    private async Task<Company> CompanyOfAsync(User user, CancellationToken cancellationToken) =>
        (await users.FindCompanyAsync(user.CompanyId, cancellationToken))!;

    private LoginResponse CreateSession(User user, Company company)
    {
        var token = tokenIssuer.Issue(user);

        return new LoginResponse(token.Value, token.ExpiresAtUtc, ToResponse(user, company));
    }

    private static CurrentUserResponse ToResponse(User user, Company company) =>
        new(user.Id, user.Email, user.DisplayName, user.Role.ToString(), company.Id, company.Name, user.EmployeeId);
}

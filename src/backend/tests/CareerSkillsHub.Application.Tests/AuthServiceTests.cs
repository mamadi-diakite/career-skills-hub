namespace CareerSkillsHub.Application.Tests;

public sealed class AuthServiceTests
{
    private const string Password = TestClock.Password;

    private readonly InMemoryUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthService _service;
    private readonly TestCompany _company;

    public AuthServiceTests()
    {
        _service = new AuthService(_users, _hasher, new FakeTokenIssuer(), new FixedTimeProvider(TestClock.Now));
        _company = _users.AddTestCompany();
    }

    [Fact]
    public async Task Sign_up_creates_a_company_with_its_first_hr_admin_and_opens_a_session()
    {
        var result = await _service.SignUpAsync(new SignUpRequest(
            " Green Valley Foods ", 120, " Jade Roux ", " Jade.Roux@Example.com ", "NewPass456", "NewPass456"));

        Assert.Empty(result.Errors);
        var company = Assert.Single(_users.Companies, c => c.Name == "Green Valley Foods");
        Assert.Equal(120, company.Headcount);
        Assert.Equal(TestClock.Now.UtcDateTime, company.CreatedAtUtc);
        var user = Assert.Single(_users.Users, u => u.Email == "jade.roux@example.com");
        Assert.Equal("Jade Roux", user.DisplayName);
        Assert.Equal(UserRole.HrAdmin, user.Role);
        Assert.Equal(company.Id, user.CompanyId);
        Assert.Null(user.EmployeeId);
        Assert.Equal("hashed:NewPass456", user.PasswordHash);
        Assert.Equal(1, _users.SaveCount);
        Assert.Equal($"token-for-{user.Id}", result.Response?.AccessToken);
        Assert.Equal(
            new CurrentUserResponse(user.Id, user.Email, "Jade Roux", "HrAdmin", company.Id, "Green Valley Foods", null),
            result.Response?.User);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Sign_up_requires_a_company_name(string companyName)
    {
        var result = await _service.SignUpAsync(new SignUpRequest(companyName, 10, "New", "new@example.com", "NewPass456", "NewPass456"));

        AssertSignUpInvalid(result, "companyName");
        Assert.Equal(["Company name is required."], result.Errors["companyName"]);
    }

    [Fact]
    public async Task Sign_up_limits_the_company_name_length()
    {
        var result = await _service.SignUpAsync(new SignUpRequest(
            new string('a', Company.NameMaxLength + 1), 10, "New", "new@example.com", "NewPass456", "NewPass456"));

        AssertSignUpInvalid(result, "companyName");
        Assert.Equal([$"Use at most {Company.NameMaxLength} characters."], result.Errors["companyName"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(Company.MaxHeadcount + 1)]
    public async Task Sign_up_requires_a_realistic_headcount(int headcount)
    {
        var result = await _service.SignUpAsync(new SignUpRequest("Company", headcount, "New", "new@example.com", "NewPass456", "NewPass456"));

        AssertSignUpInvalid(result, "headcount");
        Assert.Equal([$"Headcount must be between 1 and {Company.MaxHeadcount}."], result.Errors["headcount"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Sign_up_requires_a_name(string displayName)
    {
        var result = await _service.SignUpAsync(new SignUpRequest("Company", 10, displayName, "new@example.com", "NewPass456", "NewPass456"));

        AssertSignUpInvalid(result, "displayName");
        Assert.Equal(["Name is required."], result.Errors["displayName"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Sign_up_requires_a_valid_email(string email)
    {
        var result = await _service.SignUpAsync(new SignUpRequest("Company", 10, "New", email, "NewPass456", "NewPass456"));

        AssertSignUpInvalid(result, "email");
        Assert.Equal(["Enter a valid email address."], result.Errors["email"]);
    }

    [Fact]
    public async Task Sign_up_refuses_an_email_already_used()
    {
        var result = await _service.SignUpAsync(
            new SignUpRequest("Company", 10, "Claire", "HR@Blue.Example.com", "NewPass456", "NewPass456"));

        AssertSignUpInvalid(result, "email");
        Assert.Equal(["An account already uses this email."], result.Errors["email"]);
    }

    [Fact]
    public async Task Sign_up_applies_the_password_policy_and_confirmation()
    {
        var result = await _service.SignUpAsync(new SignUpRequest("Company", 10, "New", "new@example.com", "short", "other"));

        AssertSignUpInvalid(result, "password");
        Assert.Equal(["confirmPassword", "password"], result.Errors.Keys.Order());
    }

    [Fact]
    public async Task Sign_up_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SignUpAsync(null!));
    }

    [Fact]
    public async Task Login_returns_a_token_and_the_user_with_its_company()
    {
        var response = await _service.LoginAsync(new LoginRequest(" Manager@Blue.Example.com ", Password));

        Assert.NotNull(response);
        Assert.Equal($"token-for-{_company.Manager.Id}", response.AccessToken);
        Assert.Equal(FakeTokenIssuer.ExpiresAt, response.ExpiresAtUtc);
        Assert.Equal(
            new CurrentUserResponse(_company.Manager.Id, "manager@blue.example.com", "Marc Lefebvre", "Manager",
                _company.Company.Id, "Blue Harbour Logistics", _company.Manager.EmployeeId),
            response.User);
    }

    [Fact]
    public async Task Login_fails_with_a_wrong_password()
    {
        Assert.Null(await _service.LoginAsync(new LoginRequest("hr@blue.example.com", "wrong-password1")));
    }

    [Fact]
    public async Task Login_fails_with_an_unknown_email_and_still_hashes()
    {
        var response = await _service.LoginAsync(new LoginRequest("nobody@example.com", Password));

        Assert.Null(response);
        Assert.Equal(1, _hasher.HashCount);
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("  ", Password)]
    [InlineData("hr@blue.example.com", "")]
    public async Task Login_fails_with_blank_values(string email, string password)
    {
        Assert.Null(await _service.LoginAsync(new LoginRequest(email, password)));
        Assert.Equal(0, _hasher.HashCount);
    }

    [Fact]
    public async Task Login_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.LoginAsync(null!));
    }

    [Fact]
    public async Task Current_user_is_found_by_id()
    {
        var user = await _service.GetCurrentUserAsync(_company.Employee.Id);

        Assert.Equal("employee@blue.example.com", user?.Email);
        Assert.Equal("Employee", user?.Role);
        Assert.Equal("Blue Harbour Logistics", user?.CompanyName);
        Assert.Null(await _service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Change_password_stores_the_new_hash()
    {
        var result = await _service.ChangePasswordAsync(_company.Hr.Id, new ChangePasswordRequest(Password, "NewPass456", "NewPass456"));

        Assert.Equal(ChangePasswordStatus.Changed, result.Status);
        Assert.Empty(result.Errors);
        Assert.Equal("hashed:NewPass456", _company.Hr.PasswordHash);
        Assert.Equal(1, _users.SaveCount);
    }

    [Fact]
    public async Task Change_password_for_an_unknown_user()
    {
        var result = await _service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest(Password, "NewPass456", "NewPass456"));

        Assert.Equal(ChangePasswordStatus.UserNotFound, result.Status);
        Assert.Equal(0, _users.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("WrongPass789")]
    public async Task Change_password_checks_the_current_password(string currentPassword)
    {
        var result = await _service.ChangePasswordAsync(
            _company.Hr.Id, new ChangePasswordRequest(currentPassword, "NewPass456", "NewPass456"));

        AssertChangeInvalid(result, "currentPassword");
    }

    [Fact]
    public async Task Change_password_applies_the_password_policy()
    {
        var result = await _service.ChangePasswordAsync(_company.Hr.Id, new ChangePasswordRequest(Password, "short", "short"));

        AssertChangeInvalid(result, "newPassword");
        Assert.Contains("Use at least 8 characters.", result.Errors["newPassword"]);
    }

    [Fact]
    public async Task New_password_must_differ_from_the_current_one()
    {
        var result = await _service.ChangePasswordAsync(_company.Hr.Id, new ChangePasswordRequest(Password, Password, Password));

        AssertChangeInvalid(result, "newPassword");
        Assert.Equal(["New password must be different from the current one."], result.Errors["newPassword"]);
    }

    [Fact]
    public async Task Confirmation_must_match()
    {
        var result = await _service.ChangePasswordAsync(_company.Hr.Id, new ChangePasswordRequest(Password, "NewPass456", "NewPass457"));

        AssertChangeInvalid(result, "confirmPassword");
    }

    [Fact]
    public async Task All_errors_are_reported_together()
    {
        var result = await _service.ChangePasswordAsync(_company.Hr.Id, new ChangePasswordRequest("bad", "short", "other"));

        Assert.Equal(["confirmPassword", "currentPassword", "newPassword"], result.Errors.Keys.Order());
    }

    [Fact]
    public async Task Change_password_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ChangePasswordAsync(_company.Hr.Id, null!));
    }

    private void AssertSignUpInvalid(SignUpResult result, string field)
    {
        Assert.Null(result.Response);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Equal(3, _users.Users.Count);
        Assert.Single(_users.Companies);
        Assert.Equal(0, _users.SaveCount);
    }

    private void AssertChangeInvalid(ChangePasswordResult result, string field)
    {
        Assert.Equal(ChangePasswordStatus.Invalid, result.Status);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Equal("hashed:" + Password, _company.Hr.PasswordHash);
        Assert.Equal(0, _users.SaveCount);
    }
}

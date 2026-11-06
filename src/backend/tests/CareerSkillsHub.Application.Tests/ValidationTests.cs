namespace CareerSkillsHub.Application.Tests;

public sealed class ValidationTests
{
    [Theory]
    [InlineData("TestDemo123!")]
    [InlineData("abcdefg1")]
    public void Password_policy_accepts_a_valid_password(string password)
    {
        Assert.Empty(PasswordPolicy.Check(password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Password_policy_requires_a_password(string? password)
    {
        Assert.Equal(["Password is required."], PasswordPolicy.Check(password));
    }

    [Fact]
    public void Password_policy_checks_length_letters_and_digits()
    {
        Assert.Contains("Use at least 8 characters.", PasswordPolicy.Check("abc1"));
        Assert.Contains("Use at most 128 characters.", PasswordPolicy.Check(new string('a', 128) + "1"));
        Assert.Equal(["Use at least one letter."], PasswordPolicy.Check("12345678"));
        Assert.Equal(["Use at least one digit."], PasswordPolicy.Check("abcdefgh"));
    }

    [Fact]
    public void Field_errors_trim_values_and_report_missing_or_long_ones()
    {
        var errors = new FieldErrors();

        Assert.Equal("Warehouse", errors.Required("name", " Warehouse ", 20, "Name"));
        Assert.Null(errors.Required("title", " ", 20, "Title"));
        Assert.Null(errors.Required("code", "toolong", 3, "Code"));
        Assert.Equal("ok", errors.Optional("description", " ok ", 5));
        Assert.Null(errors.Optional("notes", " ", 5));
        Assert.Null(errors.Optional("summary", "too long", 5));

        Assert.Equal(["code", "summary", "title"], errors.Keys.Order());
        Assert.Equal(["Title is required."], errors["title"]);
        Assert.Equal(["Use at most 3 characters."], errors["code"]);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void Field_errors_check_ranges(int value, bool invalid)
    {
        var errors = new FieldErrors();

        errors.Range("headcount", value, 1, 10, "Headcount");

        Assert.Equal(invalid, errors.ContainsKey("headcount"));
    }

    [Fact]
    public void Service_results_carry_their_status()
    {
        var errors = new Dictionary<string, string[]> { ["name"] = ["Name is required."] };

        Assert.Equal((ServiceStatus.Ok, 3), (ServiceResult<int>.Ok(3).Status, ServiceResult<int>.Ok(3).Value));
        Assert.Equal(ServiceStatus.Unauthorized, ServiceResult<int>.Unauthorized().Status);
        Assert.Equal((ServiceStatus.Forbidden, "No"), (ServiceResult<int>.Forbidden("No").Status, ServiceResult<int>.Forbidden("No").Message));
        Assert.Equal((ServiceStatus.NotFound, "Gone"), (ServiceResult<int>.NotFound("Gone").Status, ServiceResult<int>.NotFound("Gone").Message));
        Assert.Equal((ServiceStatus.Conflict, "Used"), (ServiceResult<int>.Conflict("Used").Status, ServiceResult<int>.Conflict("Used").Message));
        Assert.Same(errors, ServiceResult<int>.Invalid(errors).Errors);
        Assert.Empty(ServiceResult<int>.Ok(3).Errors);
    }

    [Fact]
    public void Registers_the_application_services()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IUserRepository, InMemoryUserRepository>()
            .AddSingleton<IPasswordHasher, FakePasswordHasher>()
            .AddSingleton<IAccessTokenIssuer, FakeTokenIssuer>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<ActorResolver>());
        Assert.NotNull(scope.ServiceProvider.GetService<AuthService>());
        Assert.NotNull(scope.ServiceProvider.GetService<CompanyService>());
    }
}

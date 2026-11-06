namespace CareerSkillsHub.Domain.Tests;

public sealed class AccountTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    [Fact]
    public void Creates_a_company_with_its_headcount()
    {
        var company = new Company(" Blue Harbour Logistics ", 64, TestDates.Now);

        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal("Blue Harbour Logistics", company.Name);
        Assert.Equal(64, company.Headcount);
        Assert.Equal(TestDates.Now, company.CreatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Company.MaxHeadcount + 1)]
    public void Company_headcount_is_between_one_and_the_maximum(int headcount)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new Company("Blue Harbour", headcount, TestDates.Now));

        Assert.Equal("headcount", error.ParamName);
    }

    [Fact]
    public void Company_rejects_blank_name_and_local_date()
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => new Company(" ", 10, TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new Company("Blue Harbour", 10, TestDates.Local)).ParamName);
    }

    [Fact]
    public void Creates_an_hr_admin_without_employee_record()
    {
        var user = new User(CompanyId, " Claire.Fontaine@Example.com ", " Claire Fontaine ", "hash", UserRole.HrAdmin, null, TestDates.Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(CompanyId, user.CompanyId);
        Assert.Equal("claire.fontaine@example.com", user.Email);
        Assert.Equal("Claire Fontaine", user.DisplayName);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(UserRole.HrAdmin, user.Role);
        Assert.Null(user.EmployeeId);
        Assert.Equal(TestDates.Now, user.CreatedAtUtc);
    }

    [Theory]
    [InlineData(UserRole.HrAdmin)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Employee)]
    public void Any_role_can_be_linked_to_an_employee_record(UserRole role)
    {
        Assert.Equal(EmployeeId, new User(CompanyId, "a@example.com", "A", "hash", role, EmployeeId, TestDates.Now).EmployeeId);
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Employee)]
    public void Managers_and_employees_need_an_employee_record(UserRole role)
    {
        var error = Assert.Throws<ArgumentException>(
            () => new User(CompanyId, "a@example.com", "A", "hash", role, null, TestDates.Now));

        Assert.Equal("employeeId", error.ParamName);
    }

    [Fact]
    public void User_rejects_invalid_values()
    {
        Assert.Equal("companyId", Assert.Throws<ArgumentException>(
            () => new User(Guid.Empty, "a@example.com", "A", "hash", UserRole.HrAdmin, null, TestDates.Now)).ParamName);
        Assert.Equal("email", Assert.Throws<ArgumentException>(
            () => new User(CompanyId, "a@", "A", "hash", UserRole.HrAdmin, null, TestDates.Now)).ParamName);
        Assert.Equal("displayName", Assert.Throws<ArgumentException>(
            () => new User(CompanyId, "a@example.com", " ", "hash", UserRole.HrAdmin, null, TestDates.Now)).ParamName);
        Assert.Equal("passwordHash", Assert.Throws<ArgumentException>(
            () => new User(CompanyId, "a@example.com", "A", "", UserRole.HrAdmin, null, TestDates.Now)).ParamName);
        Assert.Equal("role", Assert.Throws<ArgumentOutOfRangeException>(
            () => new User(CompanyId, "a@example.com", "A", "hash", (UserRole)9, null, TestDates.Now)).ParamName);
        Assert.Equal("employeeId", Assert.Throws<ArgumentException>(
            () => new User(CompanyId, "a@example.com", "A", "hash", UserRole.HrAdmin, Guid.Empty, TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new User(CompanyId, "a@example.com", "A", "hash", UserRole.HrAdmin, null, TestDates.Local)).ParamName);
    }
}

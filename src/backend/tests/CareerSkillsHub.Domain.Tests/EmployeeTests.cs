namespace CareerSkillsHub.Domain.Tests;

public sealed class EmployeeTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid JobId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly DateOnly HireDate = new(2021, 3, 1);

    [Fact]
    public void Creates_an_employee_with_a_first_job_from_the_hire_date()
    {
        var managerId = Guid.NewGuid();

        var employee = new Employee(
            CompanyId, " Hugo ", " Lambert ", " Hugo.Lambert@Example.com ", HireDate, JobId, DepartmentId, managerId, TestDates.Now);

        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.Equal(CompanyId, employee.CompanyId);
        Assert.Equal("Hugo", employee.FirstName);
        Assert.Equal("Lambert", employee.LastName);
        Assert.Equal("Hugo Lambert", employee.FullName);
        Assert.Equal("hugo.lambert@example.com", employee.Email);
        Assert.Equal(HireDate, employee.HireDate);
        Assert.Equal(JobId, employee.JobId);
        Assert.Equal(DepartmentId, employee.DepartmentId);
        Assert.Equal(managerId, employee.ManagerId);
        Assert.Equal(TestDates.Now, employee.CreatedAtUtc);
        var first = Assert.Single(employee.JobHistory);
        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.Equal(employee.Id, first.EmployeeId);
        Assert.Equal(JobId, first.JobId);
        Assert.Equal(DepartmentId, first.DepartmentId);
        Assert.Equal(HireDate, first.StartDate);
        Assert.Null(first.EndDate);
    }

    [Fact]
    public void Manager_is_optional()
    {
        Assert.Null(NewEmployee().ManagerId);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("companyId", Assert.Throws<ArgumentException>(() => NewEmployee(companyId: Guid.Empty)).ParamName);
        Assert.Equal("firstName", Assert.Throws<ArgumentException>(() => NewEmployee(firstName: " ")).ParamName);
        Assert.Equal("lastName", Assert.Throws<ArgumentException>(() => NewEmployee(lastName: "")).ParamName);
        Assert.Equal("email", Assert.Throws<ArgumentException>(() => NewEmployee(email: "hugo@")).ParamName);
        Assert.Equal("jobId", Assert.Throws<ArgumentException>(() => NewEmployee(jobId: Guid.Empty)).ParamName);
        Assert.Equal("departmentId", Assert.Throws<ArgumentException>(() => NewEmployee(departmentId: Guid.Empty)).ParamName);
        Assert.Equal("managerId", Assert.Throws<ArgumentException>(() => NewEmployee(managerId: Guid.Empty)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(() => NewEmployee(createdAtUtc: TestDates.Local)).ParamName);
    }

    [Theory]
    [InlineData("hugo.lambert@example.com", true)]
    [InlineData(" Hugo.Lambert@Example.com ", true)]
    [InlineData("hugo.lambert", false)]
    [InlineData(null, false)]
    public void Checks_an_email_before_creating(string? email, bool expected)
    {
        Assert.Equal(expected, Employee.IsValidEmail(email));
    }

    [Fact]
    public void Job_change_closes_the_current_job_and_keeps_the_history()
    {
        var employee = NewEmployee();
        Guid teamLead = Guid.NewGuid(), transport = Guid.NewGuid();

        employee.ChangeJob(teamLead, DepartmentId, new DateOnly(2023, 9, 1));
        employee.ChangeJob(teamLead, transport, new DateOnly(2025, 1, 15));

        Assert.Equal(teamLead, employee.JobId);
        Assert.Equal(transport, employee.DepartmentId);
        Assert.Equal(
            [
                (JobId, DepartmentId, HireDate, (DateOnly?)new DateOnly(2023, 8, 31)),
                (teamLead, DepartmentId, new DateOnly(2023, 9, 1), new DateOnly(2025, 1, 14)),
                (teamLead, transport, new DateOnly(2025, 1, 15), null),
            ],
            employee.JobHistory.Select(a => (a.JobId, a.DepartmentId, a.StartDate, a.EndDate)));
    }

    [Theory]
    [InlineData(2021, 3, 1)]
    [InlineData(2020, 12, 31)]
    public void Job_change_starts_after_the_current_job(int year, int month, int day)
    {
        var employee = NewEmployee();

        var error = Assert.Throws<ArgumentException>(
            () => employee.ChangeJob(Guid.NewGuid(), DepartmentId, new DateOnly(year, month, day)));

        Assert.Equal("effectiveDate", error.ParamName);
        Assert.Single(employee.JobHistory);
    }

    [Fact]
    public void Job_change_needs_a_different_job_or_department()
    {
        var employee = NewEmployee();

        var error = Assert.Throws<ArgumentException>(() => employee.ChangeJob(JobId, DepartmentId, new DateOnly(2024, 1, 1)));

        Assert.Equal("jobId", error.ParamName);
        Assert.Single(employee.JobHistory);
    }

    [Fact]
    public void Job_change_needs_a_job_and_a_department()
    {
        var employee = NewEmployee();
        var date = new DateOnly(2024, 1, 1);

        Assert.Equal("jobId", Assert.Throws<ArgumentException>(() => employee.ChangeJob(Guid.Empty, DepartmentId, date)).ParamName);
        Assert.Equal("departmentId", Assert.Throws<ArgumentException>(() => employee.ChangeJob(JobId, Guid.Empty, date)).ParamName);
    }

    private static Employee NewEmployee(
        Guid? companyId = null,
        string firstName = "Hugo",
        string lastName = "Lambert",
        string email = "hugo.lambert@example.com",
        Guid? jobId = null,
        Guid? departmentId = null,
        Guid? managerId = null,
        DateTime? createdAtUtc = null) =>
        new(companyId ?? CompanyId, firstName, lastName, email, HireDate, jobId ?? JobId, departmentId ?? DepartmentId,
            managerId, createdAtUtc ?? TestDates.Now);
}

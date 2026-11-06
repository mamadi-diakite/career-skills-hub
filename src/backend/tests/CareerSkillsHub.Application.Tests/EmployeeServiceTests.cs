namespace CareerSkillsHub.Application.Tests;

public sealed class EmployeeServiceTests
{
    private readonly TestOrganisation _org = new();
    private readonly EmployeeService _service;

    public EmployeeServiceTests()
    {
        _service = new EmployeeService(
            _org.Actors, _org.Store, _org.Store, _org.Users, _org.PasswordHasher, new FixedTimeProvider(TestClock.Now));
    }

    [Fact]
    public async Task Hr_lists_every_employee_of_the_company_by_last_name()
    {
        var employees = (await _service.ListAsync(_org.Hr.Id)).Value!;

        Assert.Equal(["Haddad", "Lefebvre", "Moreau"], employees.Select(e => e.LastName));
    }

    [Fact]
    public async Task Manager_lists_themselves_and_their_team()
    {
        var employees = (await _service.ListAsync(_org.Manager.Id)).Value!;

        Assert.Equal([_org.Marc.Id, _org.Ines.Id], employees.Select(e => e.Id));
    }

    [Fact]
    public async Task Employee_lists_only_themselves()
    {
        var employees = (await _service.ListAsync(_org.Employee.Id)).Value!;

        Assert.Equal([_org.Ines.Id], employees.Select(e => e.Id));
    }

    [Fact]
    public async Task Summary_names_the_job_department_manager_and_account()
    {
        var employees = (await _service.ListAsync(_org.Hr.Id)).Value!;

        Assert.Equal(
            new EmployeeSummaryResponse(
                _org.Ines.Id, "Ines", "Moreau", "ines.moreau@example.com", new DateOnly(2022, 9, 1),
                _org.ForkliftOperator.Id, "Forklift operator", _org.Warehouse.Id, "Warehouse",
                _org.Marc.Id, "Marc Lefebvre", "Employee"),
            employees.Single(e => e.Id == _org.Ines.Id));
        var nadia = employees.Single(e => e.Id == _org.Nadia.Id);
        Assert.Equal(((Guid?)null, (string?)null, (string?)null), (nadia.ManagerId, nadia.ManagerName, nadia.AccountRole));
    }

    [Fact]
    public async Task Hr_reads_anyone_with_the_job_history_most_recent_first()
    {
        _org.Nadia.ChangeJob(_org.Vacant.Id, _org.Transport.Id, new DateOnly(2024, 1, 8));

        var details = (await _service.GetAsync(_org.Hr.Id, _org.Nadia.Id)).Value!;

        Assert.Equal("Dispatcher", details.Employee.JobTitle);
        Assert.Equal(
            [
                new JobAssignmentResponse(_org.Vacant.Id, "Dispatcher", _org.Transport.Id, "Transport", new DateOnly(2024, 1, 8), null),
                new JobAssignmentResponse(
                    _org.TruckDriver.Id, "Truck driver", _org.Transport.Id, "Transport", new DateOnly(2019, 4, 15), new DateOnly(2024, 1, 7)),
            ],
            details.JobHistory);
    }

    [Fact]
    public async Task Manager_reads_their_team_but_not_other_employees()
    {
        Assert.Equal(ServiceStatus.Ok, (await _service.GetAsync(_org.Manager.Id, _org.Ines.Id)).Status);
        Assert.Equal(ServiceStatus.Ok, (await _service.GetAsync(_org.Manager.Id, _org.Marc.Id)).Status);

        var other = await _service.GetAsync(_org.Manager.Id, _org.Nadia.Id);

        Assert.Equal(ServiceStatus.Forbidden, other.Status);
        Assert.Equal(AccessRules.EmployeeIsNotVisible, other.Message);
    }

    [Fact]
    public async Task Employee_reads_only_themselves()
    {
        Assert.Equal(ServiceStatus.Ok, (await _service.GetAsync(_org.Employee.Id, _org.Ines.Id)).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.GetAsync(_org.Employee.Id, _org.Marc.Id)).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.GetAsync(_org.Employee.Id, _org.Nadia.Id)).Status);
    }

    [Fact]
    public async Task Employees_of_another_company_are_not_found()
    {
        var jobChange = new JobChangeRequest(_org.Vacant.Id, _org.Transport.Id, new DateOnly(2025, 1, 6));

        Assert.Equal(ServiceStatus.NotFound, (await _service.GetAsync(_org.Hr.Id, _org.OtherEmployee.Id)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.ChangeJobAsync(_org.Hr.Id, _org.OtherEmployee.Id, jobChange)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.InviteAsync(_org.Hr.Id, _org.OtherEmployee.Id, new InvitationRequest("Employee"))).Status);
        Assert.Equal([_org.OtherEmployee.Id], (await _service.ListAsync(_org.OtherHr.Id)).Value!.Select(e => e.Id));
    }

    [Fact]
    public async Task Unknown_user_is_unauthorized()
    {
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.ListAsync(Guid.NewGuid())).Status);
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.GetAsync(Guid.NewGuid(), _org.Ines.Id)).Status);
    }

    [Fact]
    public async Task Managers_and_employees_cannot_change_employees()
    {
        foreach (var user in _org.NonHrUsers)
        {
            var results = new[]
            {
                (await _service.CreateAsync(user.Id, NewEmployeeRequest())).Status,
                (await _service.ChangeJobAsync(user.Id, _org.Ines.Id, new JobChangeRequest(_org.TeamLead.Id, _org.Warehouse.Id, new DateOnly(2025, 1, 6)))).Status,
                (await _service.InviteAsync(user.Id, _org.Nadia.Id, new InvitationRequest("HrAdmin"))).Status,
            };

            Assert.All(results, status => Assert.Equal(ServiceStatus.Forbidden, status));
        }

        Assert.Equal(0, _org.Store.SaveCount + _org.Users.SaveCount);
    }

    [Fact]
    public async Task Hr_creates_an_employee_with_a_first_job()
    {
        var details = (await _service.CreateAsync(_org.Hr.Id, NewEmployeeRequest() with { Email = " Lucas.Bernard@Example.com " })).Value!;

        Assert.Equal(("lucas.bernard@example.com", "Marc Lefebvre", (string?)null), (details.Employee.Email, details.Employee.ManagerName, details.Employee.AccountRole));
        Assert.Equal(
            [new JobAssignmentResponse(_org.ForkliftOperator.Id, "Forklift operator", _org.Warehouse.Id, "Warehouse", new DateOnly(2023, 3, 6), null)],
            details.JobHistory);
        var stored = Assert.Single(_org.Store.Employees, e => e.Id == details.Employee.Id);
        Assert.Equal((_org.Company.Id, TestClock.Now.UtcDateTime), (stored.CompanyId, stored.CreatedAtUtc));
        Assert.Equal(1, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Invalid_employee_is_refused()
    {
        var result = await _service.CreateAsync(
            _org.Hr.Id,
            new CreateEmployeeRequest(" ", "", "not-an-email", null, _org.OtherJob.Id, _org.OtherDepartment.Id, _org.OtherEmployee.Id));

        Assert.Equal(
            ["departmentId", "email", "firstName", "hireDate", "jobId", "lastName", "managerId"],
            result.Errors.Keys.Order());
        Assert.Equal(0, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Employee_email_is_unique_in_the_company()
    {
        var result = await _service.CreateAsync(_org.Hr.Id, NewEmployeeRequest() with { Email = "NADIA.haddad@example.com", ManagerId = null });

        Assert.Equal(["An employee already has this email."], result.Errors["email"]);
    }

    [Fact]
    public async Task Hr_changes_the_job_and_keeps_the_history()
    {
        var details = (await _service.ChangeJobAsync(
            _org.Hr.Id, _org.Ines.Id, new JobChangeRequest(_org.TeamLead.Id, _org.Warehouse.Id, new DateOnly(2025, 1, 6)))).Value!;

        Assert.Equal(("Warehouse team lead", 2), (details.Employee.JobTitle, details.JobHistory.Count));
        Assert.Equal(new DateOnly(2025, 1, 5), details.JobHistory[1].EndDate);
        Assert.Equal(1, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Job_change_needs_a_new_job_and_a_later_date()
    {
        var same = await _service.ChangeJobAsync(
            _org.Hr.Id, _org.Ines.Id, new JobChangeRequest(_org.ForkliftOperator.Id, _org.Warehouse.Id, new DateOnly(2022, 9, 1)));
        var unknown = await _service.ChangeJobAsync(
            _org.Hr.Id, _org.Ines.Id, new JobChangeRequest(_org.OtherJob.Id, _org.OtherDepartment.Id, null));

        Assert.Equal(["effectiveDate", "jobId"], same.Errors.Keys.Order());
        Assert.Equal(["The change must start after 2022-09-01, when the current job started."], same.Errors["effectiveDate"]);
        Assert.Equal(["departmentId", "effectiveDate", "jobId"], unknown.Errors.Keys.Order());
        Assert.Equal(["Effective date is required."], unknown.Errors["effectiveDate"]);
        Assert.Single(_org.Ines.JobHistory);
    }

    [Fact]
    public async Task Moving_to_another_department_in_the_same_job_is_a_change()
    {
        var result = await _service.ChangeJobAsync(
            _org.Hr.Id, _org.Ines.Id, new JobChangeRequest(_org.ForkliftOperator.Id, _org.Transport.Id, new DateOnly(2025, 1, 6)));

        Assert.Equal("Transport", result.Value!.Employee.DepartmentName);
    }

    [Theory]
    [InlineData("Manager", UserRole.Manager)]
    [InlineData(" hradmin ", UserRole.HrAdmin)]
    public async Task Hr_invites_an_employee_with_a_temporary_password(string role, UserRole expected)
    {
        var invitation = (await _service.InviteAsync(_org.Hr.Id, _org.Nadia.Id, new InvitationRequest(role))).Value!;

        Assert.Equal((_org.Nadia.Id, "nadia.haddad@example.com", expected.ToString()), (invitation.EmployeeId, invitation.Email, invitation.Role));
        Assert.Empty(PasswordPolicy.Check(invitation.TemporaryPassword));
        var account = Assert.Single(_org.Users.Users, u => u.EmployeeId == _org.Nadia.Id);
        Assert.Equal(("Nadia Haddad", expected, "hashed:" + invitation.TemporaryPassword), (account.DisplayName, account.Role, account.PasswordHash));
        Assert.Equal(1, _org.Users.SaveCount);
    }

    [Fact]
    public async Task Invitation_needs_a_known_role()
    {
        var result = await _service.InviteAsync(_org.Hr.Id, _org.Nadia.Id, new InvitationRequest("Owner"));

        Assert.Equal(["Choose HrAdmin, Manager or Employee."], result.Errors["role"]);
    }

    [Fact]
    public async Task Employee_with_an_account_is_not_invited_twice()
    {
        var result = await _service.InviteAsync(_org.Hr.Id, _org.Ines.Id, new InvitationRequest("Employee"));

        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.Equal("This employee already has an account.", result.Message);
    }

    [Fact]
    public async Task Invitation_is_refused_when_the_email_has_an_account()
    {
        _org.Users.Add(new User(
            _org.OtherCompany.Id, "nadia.haddad@example.com", "Nadia Haddad", "hashed:x", UserRole.HrAdmin, null, TestClock.Now.UtcDateTime));

        var result = await _service.InviteAsync(_org.Hr.Id, _org.Nadia.Id, new InvitationRequest("Employee"));

        Assert.Equal("An account already uses the email of this employee.", result.Message);
        Assert.Equal(0, _org.Users.SaveCount);
    }

    [Fact]
    public async Task Missing_request_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(_org.Hr.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ChangeJobAsync(_org.Hr.Id, _org.Ines.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.InviteAsync(_org.Hr.Id, _org.Ines.Id, null!));
    }

    private CreateEmployeeRequest NewEmployeeRequest() =>
        new("Lucas", "Bernard", "lucas.bernard@example.com", new DateOnly(2023, 3, 6), _org.ForkliftOperator.Id, _org.Warehouse.Id, _org.Marc.Id);
}

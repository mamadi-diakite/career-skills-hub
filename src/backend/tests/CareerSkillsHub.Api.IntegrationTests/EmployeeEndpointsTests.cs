namespace CareerSkillsHub.Api.IntegrationTests;

/// <summary>AUTH-6: the API checks on every request who may see which employee.</summary>
public sealed class EmployeeEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Hr_lists_every_employee()
    {
        var client = await factory.LoggedInAsync(ApiClients.Hr);

        var employees = await client.GetFromJsonAsync<List<EmployeeSummaryResponse>>("/api/employees");

        Assert.Equal(DemoCompany.Employees.Count, employees!.Count);
    }

    [Fact]
    public async Task Manager_lists_themselves_and_their_team_only()
    {
        var client = await factory.LoggedInAsync(ApiClients.Manager);

        var employees = await client.GetFromJsonAsync<List<EmployeeSummaryResponse>>("/api/employees");

        Assert.Equal(["Bernard", "Girard", "Lefebvre", "Moreau"], employees!.Select(e => e.LastName));
    }

    [Fact]
    public async Task Employee_lists_only_themselves()
    {
        var client = await factory.LoggedInAsync(ApiClients.Employee);

        var employees = await client.GetFromJsonAsync<List<EmployeeSummaryResponse>>("/api/employees");

        var self = Assert.Single(employees!);
        Assert.Equal(("ines.moreau@example.com", "Employee", "Marc Lefebvre"), (self.Email, self.AccountRole, self.ManagerName));
    }

    [Fact]
    public async Task Manager_reads_team_members_but_not_other_employees()
    {
        var ids = await EmployeeIdsAsync();
        var client = await factory.LoggedInAsync(ApiClients.Manager);

        var tom = await client.GetAsync($"/api/employees/{ids["Girard"]}").ReadAsync<EmployeeDetailsResponse>();

        Assert.Equal(["Transport", "Warehouse"], tom.JobHistory.Select(a => a.DepartmentName).Order());
        Assert.Equal(HttpStatusCode.OK, await client.GetAsync($"/api/employees/{ids["Lefebvre"]}").StatusAsync());
        foreach (var outsider in new[] { "Haddad", "Garnier", "Fontaine" })
        {
            var problem = await client.GetAsync($"/api/employees/{ids[outsider]}").ReadAsync<ProblemDetails>(HttpStatusCode.Forbidden);
            Assert.Equal("You can only see your own record and your team.", problem.Title);
        }
    }

    [Fact]
    public async Task Employee_reads_only_their_own_record()
    {
        var ids = await EmployeeIdsAsync();
        var client = await factory.LoggedInAsync(ApiClients.Employee);

        Assert.Equal(HttpStatusCode.OK, await client.GetAsync($"/api/employees/{ids["Moreau"]}").StatusAsync());
        foreach (var other in new[] { "Lefebvre", "Girard", "Haddad", "Fontaine" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, await client.GetAsync($"/api/employees/{ids[other]}").StatusAsync());
        }
    }

    [Fact]
    public async Task Hr_of_another_company_sees_no_demo_employee()
    {
        var ids = await EmployeeIdsAsync();
        var other = await factory.SignedUpAsync("Riverside Print", "omar.saidi@example.com");

        Assert.Empty((await other.GetFromJsonAsync<List<EmployeeSummaryResponse>>("/api/employees"))!);
        Assert.Equal(HttpStatusCode.NotFound, await other.GetAsync($"/api/employees/{ids["Moreau"]}").StatusAsync());
        Assert.Equal(HttpStatusCode.NotFound, await other.PostAsJsonAsync(
            $"/api/employees/{ids["Haddad"]}/invitation", new InvitationRequest("HrAdmin")).StatusAsync());
    }

    [Theory]
    [InlineData(ApiClients.Manager)]
    [InlineData(ApiClients.Employee)]
    public async Task Managers_and_employees_cannot_change_employees(string email)
    {
        var ids = await EmployeeIdsAsync();
        var hr = await factory.LoggedInAsync(ApiClients.Hr);
        var ines = await hr.GetAsync($"/api/employees/{ids["Moreau"]}").ReadAsync<EmployeeDetailsResponse>();
        var client = await factory.LoggedInAsync(email);

        var statuses = new[]
        {
            await client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
                "Eva", "Blanc", "eva.blanc@example.com", new DateOnly(2025, 1, 6), ines.Employee.JobId, ines.Employee.DepartmentId, null)).StatusAsync(),
            await client.PostAsJsonAsync($"/api/employees/{ids["Moreau"]}/job-changes", new JobChangeRequest(
                ines.Employee.JobId, ines.Employee.DepartmentId, new DateOnly(2025, 1, 6))).StatusAsync(),
            await client.PostAsJsonAsync($"/api/employees/{ids["Haddad"]}/invitation", new InvitationRequest("HrAdmin")).StatusAsync(),
        };

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Forbidden, status));
        Assert.Equal(DemoCompany.Employees.Count, (await hr.GetFromJsonAsync<List<EmployeeSummaryResponse>>("/api/employees"))!.Count);
    }

    [Fact]
    public async Task Hr_hires_moves_and_invites_an_employee_who_then_sees_only_themselves()
    {
        var hr = await factory.SignedUpAsync("Orchard Tools", "nina.perrin@example.com");
        var assembly = await hr.PostAsJsonAsync("/api/departments", new DepartmentRequest("Assembly")).ReadAsync<DepartmentResponse>(HttpStatusCode.Created);
        var fitter = await hr.PostAsJsonAsync("/api/jobs", new JobRequest("Fitter", assembly.Id, null)).ReadAsync<JobResponse>(HttpStatusCode.Created);
        var lead = await hr.PostAsJsonAsync("/api/jobs", new JobRequest("Line lead", assembly.Id, null)).ReadAsync<JobResponse>(HttpStatusCode.Created);

        var created = await hr.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "Eva", "Blanc", "eva.blanc@example.com", new DateOnly(2023, 3, 6), fitter.Id, assembly.Id, null))
            .ReadAsync<EmployeeDetailsResponse>(HttpStatusCode.Created);
        var colleague = await hr.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "Noah", "Petit", "noah.petit@example.com", new DateOnly(2024, 2, 5), fitter.Id, assembly.Id, created.Employee.Id))
            .ReadAsync<EmployeeDetailsResponse>(HttpStatusCode.Created);
        var moved = await hr.PostAsJsonAsync($"/api/employees/{created.Employee.Id}/job-changes", new JobChangeRequest(
            lead.Id, assembly.Id, new DateOnly(2025, 1, 6))).ReadAsync<EmployeeDetailsResponse>();
        var invitation = await hr.PostAsJsonAsync($"/api/employees/{created.Employee.Id}/invitation", new InvitationRequest("Manager"))
            .ReadAsync<InvitationResponse>();

        Assert.Equal(["Line lead", "Fitter"], moved.JobHistory.Select(a => a.JobTitle));
        Assert.Equal("Eva Blanc", colleague.Employee.ManagerName);
        Assert.Equal(HttpStatusCode.Conflict, await hr.PostAsJsonAsync(
            $"/api/employees/{created.Employee.Id}/invitation", new InvitationRequest("Manager")).StatusAsync());

        var eva = await factory.LoggedInAsync("eva.blanc@example.com", invitation.TemporaryPassword);
        var team = await eva.GetFromJsonAsync<List<EmployeeSummaryResponse>>("/api/employees");
        Assert.Equal(["Blanc", "Petit"], team!.Select(e => e.LastName));
        Assert.Equal(HttpStatusCode.Forbidden, await eva.PostAsJsonAsync("/api/departments", new DepartmentRequest("Mine")).StatusAsync());
        Assert.Equal(HttpStatusCode.Conflict, await hr.DeleteAsync($"/api/jobs/{fitter.Id}").StatusAsync());
    }

    [Fact]
    public async Task Invalid_employee_returns_a_validation_problem()
    {
        var hr = await factory.LoggedInAsync(ApiClients.Hr);

        var problem = await hr.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "", "Blanc", "not-an-email", null, Guid.NewGuid(), Guid.NewGuid(), null)).ReadAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);

        Assert.Equal(["departmentId", "email", "firstName", "hireDate", "jobId"], problem.Errors.Keys.Order());
    }

    private async Task<Dictionary<string, Guid>> EmployeeIdsAsync()
    {
        var hr = await factory.LoggedInAsync(ApiClients.Hr);
        var employees = await hr.GetFromJsonAsync<List<EmployeeSummaryResponse>>("/api/employees");

        return employees!.ToDictionary(e => e.LastName, e => e.Id);
    }
}

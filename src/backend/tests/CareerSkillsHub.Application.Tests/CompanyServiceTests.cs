namespace CareerSkillsHub.Application.Tests;

public sealed class CompanyServiceTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly CompanyService _service;
    private readonly TestCompany _company;

    public CompanyServiceTests()
    {
        _service = new CompanyService(new ActorResolver(_users), _users);
        _company = _users.AddTestCompany();
        _users.AddTestCompany("Green Valley Foods");
    }

    [Fact]
    public async Task Every_role_reads_its_own_company()
    {
        foreach (var user in new[] { _company.Hr, _company.Manager, _company.Employee })
        {
            var result = await _service.GetAsync(user.Id);

            Assert.Equal(ServiceStatus.Ok, result.Status);
            Assert.Equal(new CompanyResponse(_company.Company.Id, "Blue Harbour Logistics", 64), result.Value);
        }
    }

    [Fact]
    public async Task Unknown_user_is_unauthorized()
    {
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.GetAsync(Guid.NewGuid())).Status);
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.UpdateAsync(Guid.NewGuid(), new UpdateCompanyRequest("A", 1))).Status);
    }

    [Fact]
    public async Task Hr_admin_updates_the_profile()
    {
        var result = await _service.UpdateAsync(_company.Hr.Id, new UpdateCompanyRequest(" Blue Harbour Group ", 72));

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Equal(new CompanyResponse(_company.Company.Id, "Blue Harbour Group", 72), result.Value);
        Assert.Equal(("Blue Harbour Group", 72), (_company.Company.Name, _company.Company.Headcount));
        Assert.Equal(1, _users.SaveCount);
    }

    [Fact]
    public async Task Managers_and_employees_cannot_update_the_profile()
    {
        foreach (var user in new[] { _company.Manager, _company.Employee })
        {
            var result = await _service.UpdateAsync(user.Id, new UpdateCompanyRequest("Taken over", 1));

            Assert.Equal(ServiceStatus.Forbidden, result.Status);
            Assert.Equal("Only HR admins can edit the company profile.", result.Message);
        }

        Assert.Equal("Blue Harbour Logistics", _company.Company.Name);
        Assert.Equal(0, _users.SaveCount);
    }

    [Fact]
    public async Task Invalid_profile_is_reported_by_field()
    {
        var result = await _service.UpdateAsync(_company.Hr.Id, new UpdateCompanyRequest(" ", 0));

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal(["headcount", "name"], result.Errors.Keys.Order());
        Assert.Equal(0, _users.SaveCount);
    }

    [Fact]
    public async Task Update_rejects_a_missing_request()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_company.Hr.Id, null!));
    }
}

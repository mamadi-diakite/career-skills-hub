namespace CareerSkillsHub.Api.IntegrationTests;

public sealed class CompanyEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Theory]
    [InlineData(ApiClients.Hr)]
    [InlineData(ApiClients.Manager)]
    [InlineData(ApiClients.Employee)]
    public async Task Every_role_reads_its_company_profile(string email)
    {
        var client = await factory.LoggedInAsync(email);

        var company = await client.GetFromJsonAsync<CompanyResponse>("/api/company");

        Assert.Equal(DemoCompany.Name, company?.Name);
    }

    [Theory]
    [InlineData(ApiClients.Manager)]
    [InlineData(ApiClients.Employee)]
    public async Task Only_hr_edits_the_company_profile(string email)
    {
        var client = await factory.LoggedInAsync(email);

        var response = await client.PutAsJsonAsync("/api/company", new UpdateCompanyRequest("Taken Over Ltd", 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Only HR admins can edit the company profile.", problem?.Title);
        Assert.Equal(DemoCompany.Name, (await client.GetFromJsonAsync<CompanyResponse>("/api/company"))?.Name);
    }

    [Fact]
    public async Task Hr_edits_the_name_and_headcount()
    {
        var client = await factory.SignedUpAsync("Orchard Tools", "nina.perrin@example.com");

        var response = await client.PutAsJsonAsync("/api/company", new UpdateCompanyRequest(" Orchard Tools Group ", 58));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var company = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.Equal(("Orchard Tools Group", 58), (company?.Name, company?.Headcount));
        Assert.Equal(58, (await client.GetFromJsonAsync<CompanyResponse>("/api/company"))?.Headcount);
    }

    [Fact]
    public async Task Invalid_profile_returns_a_validation_problem()
    {
        var client = await factory.LoggedInAsync(ApiClients.Hr);

        var response = await client.PutAsJsonAsync("/api/company", new UpdateCompanyRequest("", -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["headcount", "name"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Hr_of_another_company_only_sees_and_edits_its_own_company()
    {
        var other = await factory.SignedUpAsync("Riverside Print", "omar.saidi@example.com");

        await other.PutAsJsonAsync("/api/company", new UpdateCompanyRequest("Riverside Print Works", 20));

        Assert.Equal("Riverside Print Works", (await other.GetFromJsonAsync<CompanyResponse>("/api/company"))?.Name);
        var demoHr = await factory.LoggedInAsync(ApiClients.Hr);
        Assert.Equal(DemoCompany.Name, (await demoHr.GetFromJsonAsync<CompanyResponse>("/api/company"))?.Name);
    }
}

namespace CareerSkillsHub.Api.IntegrationTests;

public sealed class OrganisationEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private static readonly SkillLevelTexts Levels = new("Knows the basics", "Works alone", "Handles hard cases", "Teaches others");

    [Theory]
    [InlineData(ApiClients.Hr)]
    [InlineData(ApiClients.Manager)]
    [InlineData(ApiClients.Employee)]
    public async Task Every_role_reads_departments_jobs_and_the_skills_framework(string email)
    {
        var client = await factory.LoggedInAsync(email);

        var departments = await client.GetFromJsonAsync<List<DepartmentResponse>>("/api/departments");
        var domains = await client.GetFromJsonAsync<List<SkillDomainResponse>>("/api/skill-domains");
        var jobs = await client.GetFromJsonAsync<List<JobResponse>>("/api/jobs");
        var forkliftOperator = await client.GetFromJsonAsync<JobResponse>($"/api/jobs/{jobs!.Single(j => j.Title == "Forklift operator").Id}");

        Assert.Equal(DemoCompany.Departments.Order(), departments!.Select(d => d.Name));
        Assert.Equal(DemoCompany.SkillDomains.Count, domains!.Count);
        Assert.Equal(DemoCompany.Jobs.Count, jobs!.Count);
        Assert.Contains(forkliftOperator!.RequiredSkills, r => r.SkillName == "Forklift driving");
        Assert.All(forkliftOperator.RequiredSkills, r => Assert.Contains(r.ExpectedLevel, new[] { "Beginner", "Intermediate", "Advanced", "Expert" }));
    }

    [Theory]
    [InlineData(ApiClients.Manager)]
    [InlineData(ApiClients.Employee)]
    public async Task Managers_and_employees_cannot_change_the_organisation(string email)
    {
        var hr = await factory.LoggedInAsync(ApiClients.Hr);
        var departmentId = (await hr.GetFromJsonAsync<List<DepartmentResponse>>("/api/departments"))![0].Id;
        var domain = (await hr.GetFromJsonAsync<List<SkillDomainResponse>>("/api/skill-domains"))![0];
        var jobId = (await hr.GetFromJsonAsync<List<JobResponse>>("/api/jobs"))![0].Id;
        var client = await factory.LoggedInAsync(email);
        var skillRequest = new SkillRequest(domain.Id, "Taken over", null, Levels);

        var statuses = new[]
        {
            await client.PostAsJsonAsync("/api/departments", new DepartmentRequest("Taken over")).StatusAsync(),
            await client.PutAsJsonAsync($"/api/departments/{departmentId}", new DepartmentRequest("Taken over")).StatusAsync(),
            await client.DeleteAsync($"/api/departments/{departmentId}").StatusAsync(),
            await client.PostAsJsonAsync("/api/skill-domains", new SkillDomainRequest("Taken over", null)).StatusAsync(),
            await client.PutAsJsonAsync($"/api/skill-domains/{domain.Id}", new SkillDomainRequest("Taken over", null)).StatusAsync(),
            await client.DeleteAsync($"/api/skill-domains/{domain.Id}").StatusAsync(),
            await client.PostAsJsonAsync("/api/skills", skillRequest).StatusAsync(),
            await client.PutAsJsonAsync($"/api/skills/{domain.Skills[0].Id}", skillRequest).StatusAsync(),
            await client.DeleteAsync($"/api/skills/{domain.Skills[0].Id}").StatusAsync(),
            await client.PostAsJsonAsync("/api/jobs", new JobRequest("Taken over", departmentId, null)).StatusAsync(),
            await client.PutAsJsonAsync($"/api/jobs/{jobId}", new JobRequest("Taken over", departmentId, null)).StatusAsync(),
            await client.PutAsJsonAsync($"/api/jobs/{jobId}/required-skills", new RequiredSkillsRequest([])).StatusAsync(),
            await client.DeleteAsync($"/api/jobs/{jobId}").StatusAsync(),
        };

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Forbidden, status));
        Assert.DoesNotContain(await hr.GetFromJsonAsync<List<DepartmentResponse>>("/api/departments") ?? [], d => d.Name == "Taken over");
    }

    [Fact]
    public async Task Hr_builds_departments_and_the_skills_framework()
    {
        var hr = await factory.SignedUpAsync("Orchard Tools", "nina.perrin@example.com");

        var department = await hr.PostAsJsonAsync("/api/departments", new DepartmentRequest("Assembly")).ReadAsync<DepartmentResponse>(HttpStatusCode.Created);
        var renamed = await hr.PutAsJsonAsync($"/api/departments/{department.Id}", new DepartmentRequest("Final assembly")).ReadAsync<DepartmentResponse>();
        var domain = await hr.PostAsJsonAsync("/api/skill-domains", new SkillDomainRequest("Safety", null)).ReadAsync<SkillDomainResponse>(HttpStatusCode.Created);
        var updatedDomain = await hr.PutAsJsonAsync($"/api/skill-domains/{domain.Id}", new SkillDomainRequest("Safety", "Rules on site")).ReadAsync<SkillDomainResponse>();
        var skill = await hr.PostAsJsonAsync("/api/skills", new SkillRequest(domain.Id, "Machine guarding", null, Levels)).ReadAsync<SkillResponse>(HttpStatusCode.Created);
        var updatedSkill = await hr.PutAsJsonAsync($"/api/skills/{skill.Id}", new SkillRequest(domain.Id, "Machine guarding", "Guards and interlocks", Levels)).ReadAsync<SkillResponse>();

        Assert.Equal("Final assembly", renamed.Name);
        Assert.Equal("Rules on site", updatedDomain.Description);
        Assert.Equal(("Guards and interlocks", Levels), (updatedSkill.Description, updatedSkill.Levels));
        var framework = await hr.GetFromJsonAsync<List<SkillDomainResponse>>("/api/skill-domains");
        Assert.Equal([skill.Id], Assert.Single(framework!).Skills.Select(s => s.Id));

        Assert.Equal(HttpStatusCode.Conflict, await hr.DeleteAsync($"/api/skill-domains/{domain.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NoContent, await hr.DeleteAsync($"/api/skills/{skill.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NoContent, await hr.DeleteAsync($"/api/skill-domains/{domain.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NoContent, await hr.DeleteAsync($"/api/departments/{department.Id}").StatusAsync());
        Assert.Empty((await hr.GetFromJsonAsync<List<DepartmentResponse>>("/api/departments"))!);
    }

    [Fact]
    public async Task Hr_describes_a_job_and_its_required_skills()
    {
        var hr = await factory.SignedUpAsync("Lakeside Bakery", "paul.mercier@example.com");
        var department = await hr.PostAsJsonAsync("/api/departments", new DepartmentRequest("Production")).ReadAsync<DepartmentResponse>(HttpStatusCode.Created);
        var domain = await hr.PostAsJsonAsync("/api/skill-domains", new SkillDomainRequest("Baking", null)).ReadAsync<SkillDomainResponse>(HttpStatusCode.Created);
        var dough = await hr.PostAsJsonAsync("/api/skills", new SkillRequest(domain.Id, "Dough preparation", null, Levels)).ReadAsync<SkillResponse>(HttpStatusCode.Created);
        var hygiene = await hr.PostAsJsonAsync("/api/skills", new SkillRequest(domain.Id, "Food hygiene", null, Levels)).ReadAsync<SkillResponse>(HttpStatusCode.Created);

        var job = await hr.PostAsJsonAsync("/api/jobs", new JobRequest("Baker", department.Id, null)).ReadAsync<JobResponse>(HttpStatusCode.Created);
        await hr.PutAsJsonAsync($"/api/jobs/{job.Id}", new JobRequest("Bread baker", department.Id, "Bakes bread at night.")).ReadAsync<JobResponse>();
        var withSkills = await hr.PutAsJsonAsync($"/api/jobs/{job.Id}/required-skills", new RequiredSkillsRequest(
        [
            new RequiredSkillItem(dough.Id, "Advanced", "Medium"),
            new RequiredSkillItem(hygiene.Id, "Intermediate", "High"),
        ])).ReadAsync<JobResponse>();

        Assert.Equal(("Bread baker", "Production"), (withSkills.Title, withSkills.DepartmentName));
        Assert.Equal(
            [
                new RequiredSkillResponse(hygiene.Id, "Food hygiene", "Intermediate", "High"),
                new RequiredSkillResponse(dough.Id, "Dough preparation", "Advanced", "Medium"),
            ],
            withSkills.RequiredSkills);
        Assert.Equal(HttpStatusCode.Conflict, await hr.DeleteAsync($"/api/skills/{hygiene.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.Conflict, await hr.DeleteAsync($"/api/departments/{department.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NoContent, await hr.DeleteAsync($"/api/jobs/{job.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NotFound, await hr.GetAsync($"/api/jobs/{job.Id}").StatusAsync());
    }

    [Fact]
    public async Task Invalid_required_skills_return_a_validation_problem()
    {
        var hr = await factory.LoggedInAsync(ApiClients.Hr);
        var job = (await hr.GetFromJsonAsync<List<JobResponse>>("/api/jobs"))![0];

        var problem = await hr.PutAsJsonAsync($"/api/jobs/{job.Id}/required-skills", new RequiredSkillsRequest(
            [new RequiredSkillItem(Guid.NewGuid(), "Guru", "Low")])).ReadAsync<ValidationProblemDetails>(HttpStatusCode.BadRequest);

        Assert.Equal(["requiredSkills[0].expectedLevel", "requiredSkills[0].skillId"], problem.Errors.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(job.RequiredSkills.Count, (await hr.GetFromJsonAsync<JobResponse>($"/api/jobs/{job.Id}"))!.RequiredSkills.Count);
    }

    [Fact]
    public async Task Hr_of_another_company_cannot_reach_the_demo_organisation()
    {
        var demoHr = await factory.LoggedInAsync(ApiClients.Hr);
        var demoJob = (await demoHr.GetFromJsonAsync<List<JobResponse>>("/api/jobs"))![0];
        var demoDomain = (await demoHr.GetFromJsonAsync<List<SkillDomainResponse>>("/api/skill-domains"))![0];
        var other = await factory.SignedUpAsync("Riverside Print", "omar.saidi@example.com");

        Assert.Empty((await other.GetFromJsonAsync<List<JobResponse>>("/api/jobs"))!);
        Assert.Equal(HttpStatusCode.NotFound, await other.GetAsync($"/api/jobs/{demoJob.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NotFound, await other.DeleteAsync($"/api/jobs/{demoJob.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NotFound, await other.DeleteAsync($"/api/departments/{demoJob.DepartmentId}").StatusAsync());
        Assert.Equal(HttpStatusCode.NotFound, await other.DeleteAsync($"/api/skill-domains/{demoDomain.Id}").StatusAsync());
        Assert.Equal(HttpStatusCode.NotFound, await other.DeleteAsync($"/api/skills/{demoDomain.Skills[0].Id}").StatusAsync());
    }

    [Fact]
    public async Task Anonymous_requests_are_refused()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await client.GetAsync("/api/departments").StatusAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, await client.GetAsync("/api/skill-domains").StatusAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, await client.GetAsync("/api/jobs").StatusAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, await client.GetAsync("/api/employees").StatusAsync());
    }
}

namespace CareerSkillsHub.Application.Tests;

public sealed class SkillFrameworkServiceTests
{
    private static readonly SkillLevelTexts Levels = new("Basics", "Alone", "Hard cases", "Teaches");

    private readonly TestOrganisation _org = new();
    private readonly SkillFrameworkService _service;

    public SkillFrameworkServiceTests()
    {
        _service = new SkillFrameworkService(_org.Actors, _org.Store);
    }

    [Fact]
    public async Task Every_role_reads_the_framework_sorted_by_name()
    {
        foreach (var user in new[] { _org.Hr, _org.Manager, _org.Employee })
        {
            var domains = (await _service.ListAsync(user.Id)).Value!;

            Assert.Equal(["Logistics", "Safety"], domains.Select(d => d.Name));
            Assert.Equal(["Forklift driving", "Workplace safety rules"], domains[1].Skills.Select(s => s.Name));
            Assert.Equal(new SkillLevelTexts("Knows the basics", "Works alone", "Handles hard cases", "Teaches others"), domains[1].Skills[0].Levels);
        }
    }

    [Fact]
    public async Task Unknown_user_is_unauthorized()
    {
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.ListAsync(Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Managers_and_employees_cannot_change_the_framework()
    {
        var domainRequest = new SkillDomainRequest("Quality", null);
        var skillRequest = new SkillRequest(_org.Safety.Id, "First aid", null, Levels);
        foreach (var user in _org.NonHrUsers.Append(new User(_org.Company.Id, "gone@example.com", "Gone", "h", UserRole.HrAdmin, null, TestClock.Now.UtcDateTime)))
        {
            var results = new[]
            {
                (await _service.CreateDomainAsync(user.Id, domainRequest)).Status,
                (await _service.UpdateDomainAsync(user.Id, _org.Safety.Id, domainRequest)).Status,
                (await _service.DeleteDomainAsync(user.Id, _org.Safety.Id)).Status,
                (await _service.CreateSkillAsync(user.Id, skillRequest)).Status,
                (await _service.UpdateSkillAsync(user.Id, _org.Forklift.Id, skillRequest)).Status,
                (await _service.DeleteSkillAsync(user.Id, _org.Forklift.Id)).Status,
            };

            var expected = user.Role == UserRole.HrAdmin ? ServiceStatus.Unauthorized : ServiceStatus.Forbidden;
            Assert.All(results, status => Assert.Equal(expected, status));
        }

        Assert.Equal(0, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Hr_creates_and_updates_a_domain()
    {
        var created = (await _service.CreateDomainAsync(_org.Hr.Id, new SkillDomainRequest(" Quality ", " Checks "))).Value!;
        var updated = await _service.UpdateDomainAsync(_org.Hr.Id, _org.Safety.Id, new SkillDomainRequest("Health and safety", null));

        Assert.Equal(("Quality", "Checks"), (created.Name, created.Description));
        Assert.Empty(created.Skills);
        Assert.Equal("Health and safety", updated.Value!.Name);
        Assert.Equal(2, updated.Value.Skills.Count);
        Assert.Equal(2, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Invalid_domain_is_refused()
    {
        var blank = await _service.CreateDomainAsync(_org.Hr.Id, new SkillDomainRequest(" ", new string('a', SkillDomain.DescriptionMaxLength + 1)));
        var taken = await _service.UpdateDomainAsync(_org.Hr.Id, _org.Safety.Id, new SkillDomainRequest("LOGISTICS", null));
        var same = await _service.UpdateDomainAsync(_org.Hr.Id, _org.Safety.Id, new SkillDomainRequest("SAFETY", null));

        Assert.Equal(["description", "name"], blank.Errors.Keys.Order());
        Assert.Equal(["A skill domain already has this name."], taken.Errors["name"]);
        Assert.Equal(ServiceStatus.Ok, same.Status);
    }

    [Fact]
    public async Task Domain_without_skills_is_deleted_and_one_with_skills_is_kept()
    {
        var empty = (await _service.CreateDomainAsync(_org.Hr.Id, new SkillDomainRequest("Quality", null))).Value!;

        Assert.Equal(ServiceStatus.Ok, (await _service.DeleteDomainAsync(_org.Hr.Id, empty.Id)).Status);
        Assert.Equal(ServiceStatus.Conflict, (await _service.DeleteDomainAsync(_org.Hr.Id, _org.Safety.Id)).Status);
        Assert.DoesNotContain(_org.Store.SkillDomains, d => d.Id == empty.Id);
        Assert.Contains(_org.Safety, _org.Store.SkillDomains);
    }

    [Fact]
    public async Task Domains_and_skills_of_another_company_are_not_found()
    {
        var domainRequest = new SkillDomainRequest("Taken over", null);
        var skillRequest = new SkillRequest(_org.Safety.Id, "Taken over", null, Levels);

        Assert.Equal(ServiceStatus.NotFound, (await _service.UpdateDomainAsync(_org.Hr.Id, _org.OtherDomain.Id, domainRequest)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.DeleteDomainAsync(_org.Hr.Id, _org.OtherDomain.Id)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.UpdateSkillAsync(_org.Hr.Id, _org.OtherSkill.Id, skillRequest)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.DeleteSkillAsync(_org.Hr.Id, _org.OtherSkill.Id)).Status);
    }

    [Fact]
    public async Task Hr_creates_a_skill_with_its_four_levels()
    {
        var result = await _service.CreateSkillAsync(_org.Hr.Id, new SkillRequest(_org.Safety.Id, " First aid ", null, Levels));

        Assert.Equal(new SkillResponse(result.Value!.Id, _org.Safety.Id, "First aid", null, Levels), result.Value);
        Assert.Contains(_org.Store.Skills, s => s.Id == result.Value.Id && s.CompanyId == _org.Company.Id);
    }

    [Fact]
    public async Task Hr_moves_a_skill_to_another_domain()
    {
        var result = await _service.UpdateSkillAsync(
            _org.Hr.Id, _org.Forklift.Id, new SkillRequest(_org.Logistics.Id, "Forklift driving", "Indoor", Levels));

        Assert.Equal((_org.Logistics.Id, "Indoor"), (result.Value!.DomainId, result.Value.Description));
        Assert.Equal(new SkillLevelDescriptions("Basics", "Alone", "Hard cases", "Teaches"), _org.Forklift.Levels);
    }

    [Fact]
    public async Task Skill_without_levels_or_with_an_unknown_domain_is_refused()
    {
        var result = await _service.CreateSkillAsync(_org.Hr.Id, new SkillRequest(_org.OtherDomain.Id, "", null, null));

        Assert.Equal(
            ["domainId", "levels.advanced", "levels.beginner", "levels.expert", "levels.intermediate", "name"],
            result.Errors.Keys.Order());
        Assert.Equal(["The Expert level is required."], result.Errors["levels.expert"]);
    }

    [Fact]
    public async Task Skill_names_are_unique_in_the_company()
    {
        var taken = await _service.UpdateSkillAsync(
            _org.Hr.Id, _org.Forklift.Id, new SkillRequest(_org.Safety.Id, "route PLANNING", null, Levels with { Advanced = " " }));

        Assert.Equal(["A skill already has this name."], taken.Errors["name"]);
        Assert.Equal(["The Advanced level is required."], taken.Errors["levels.advanced"]);
        Assert.Equal("Forklift driving", _org.Forklift.Name);
    }

    [Fact]
    public async Task Skill_required_by_a_job_is_kept()
    {
        Assert.Equal(ServiceStatus.Conflict, (await _service.DeleteSkillAsync(_org.Hr.Id, _org.Forklift.Id)).Status);
        Assert.Contains(_org.Forklift, _org.Store.Skills);
    }

    [Fact]
    public async Task Unused_skill_is_deleted()
    {
        _org.ForkliftOperator.SetRequiredSkills([]);

        Assert.Equal(ServiceStatus.Ok, (await _service.DeleteSkillAsync(_org.Hr.Id, _org.Forklift.Id)).Status);
        Assert.DoesNotContain(_org.Forklift, _org.Store.Skills);
    }

    [Fact]
    public async Task Missing_request_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateDomainAsync(_org.Hr.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateDomainAsync(_org.Hr.Id, _org.Safety.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateSkillAsync(_org.Hr.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateSkillAsync(_org.Hr.Id, _org.Forklift.Id, null!));
    }
}

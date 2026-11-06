namespace CareerSkillsHub.Application.Tests;

public sealed class JobServiceTests
{
    private readonly TestOrganisation _org = new();
    private readonly JobService _service;

    public JobServiceTests()
    {
        _service = new JobService(_org.Actors, _org.Store);
    }

    [Fact]
    public async Task Every_role_lists_the_jobs_of_its_company_by_title()
    {
        foreach (var user in new[] { _org.Hr, _org.Manager, _org.Employee })
        {
            var jobs = (await _service.ListAsync(user.Id)).Value!;

            Assert.Equal(["Dispatcher", "Forklift operator", "Truck driver", "Warehouse team lead"], jobs.Select(j => j.Title));
        }
    }

    [Fact]
    public async Task Required_skills_come_most_critical_first_then_by_name()
    {
        var job = (await _service.GetAsync(_org.Employee.Id, _org.ForkliftOperator.Id)).Value!;

        Assert.Equal(("Forklift operator", _org.Warehouse.Id, "Warehouse"), (job.Title, job.DepartmentId, job.DepartmentName));
        Assert.Equal(
            [
                new RequiredSkillResponse(_org.Forklift.Id, "Forklift driving", "Advanced", "High"),
                new RequiredSkillResponse(_org.SafetyRules.Id, "Workplace safety rules", "Intermediate", "High"),
                new RequiredSkillResponse(_org.RoutePlanning.Id, "Route planning", "Beginner", "Low"),
            ],
            job.RequiredSkills);
    }

    [Fact]
    public async Task Jobs_of_another_company_are_not_found()
    {
        var request = new JobRequest("Taken over", _org.Warehouse.Id, null);

        Assert.Equal(ServiceStatus.NotFound, (await _service.GetAsync(_org.Hr.Id, _org.OtherJob.Id)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.UpdateAsync(_org.Hr.Id, _org.OtherJob.Id, request)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.SetRequiredSkillsAsync(_org.Hr.Id, _org.OtherJob.Id, new RequiredSkillsRequest([]))).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.DeleteAsync(_org.Hr.Id, _org.OtherJob.Id)).Status);
    }

    [Fact]
    public async Task Unknown_user_is_unauthorized()
    {
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.ListAsync(Guid.NewGuid())).Status);
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.GetAsync(Guid.NewGuid(), _org.Vacant.Id)).Status);
    }

    [Fact]
    public async Task Managers_and_employees_cannot_change_jobs()
    {
        var request = new JobRequest("Picker", _org.Warehouse.Id, null);
        foreach (var user in _org.NonHrUsers)
        {
            var results = new[]
            {
                (await _service.CreateAsync(user.Id, request)).Status,
                (await _service.UpdateAsync(user.Id, _org.Vacant.Id, request)).Status,
                (await _service.SetRequiredSkillsAsync(user.Id, _org.Vacant.Id, new RequiredSkillsRequest([]))).Status,
                (await _service.DeleteAsync(user.Id, _org.Vacant.Id)).Status,
            };

            Assert.All(results, status => Assert.Equal(ServiceStatus.Forbidden, status));
        }

        Assert.Equal(0, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Hr_creates_and_updates_a_job()
    {
        var created = (await _service.CreateAsync(_org.Hr.Id, new JobRequest(" Picker ", _org.Warehouse.Id, " Prepares orders "))).Value!;
        var updated = (await _service.UpdateAsync(_org.Hr.Id, created.Id, new JobRequest("PICKER", _org.Transport.Id, null))).Value!;

        Assert.Equal(("Picker", _org.Warehouse.Id, "Warehouse", "Prepares orders"), (created.Title, created.DepartmentId, created.DepartmentName, created.Description));
        Assert.Equal((created.Id, "PICKER", "Transport", (string?)null), (updated.Id, updated.Title, updated.DepartmentName, updated.Description));
        Assert.Empty(updated.RequiredSkills);
        Assert.Equal(2, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Invalid_job_is_refused()
    {
        var blank = await _service.CreateAsync(
            _org.Hr.Id, new JobRequest(" ", _org.OtherDepartment.Id, new string('a', Job.DescriptionMaxLength + 1)));
        var taken = await _service.UpdateAsync(_org.Hr.Id, _org.Vacant.Id, new JobRequest("truck driver", _org.Transport.Id, null));

        Assert.Equal(["departmentId", "description", "title"], blank.Errors.Keys.Order());
        Assert.Equal(["A job already has this title."], taken.Errors["title"]);
        Assert.Equal("Dispatcher", _org.Vacant.Title);
    }

    [Fact]
    public async Task Hr_replaces_the_required_skills()
    {
        var result = await _service.SetRequiredSkillsAsync(_org.Hr.Id, _org.ForkliftOperator.Id, new RequiredSkillsRequest(
        [
            new RequiredSkillItem(_org.Forklift.Id, " expert ", "medium"),
        ]));

        Assert.Equal([new RequiredSkillResponse(_org.Forklift.Id, "Forklift driving", "Expert", "Medium")], result.Value!.RequiredSkills);
        Assert.Equal(1, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Missing_list_clears_the_required_skills()
    {
        var result = await _service.SetRequiredSkillsAsync(_org.Hr.Id, _org.ForkliftOperator.Id, new RequiredSkillsRequest(null));

        Assert.Empty(result.Value!.RequiredSkills);
        Assert.Empty(_org.ForkliftOperator.RequiredSkills);
    }

    [Fact]
    public async Task Invalid_required_skills_are_reported_by_item()
    {
        var result = await _service.SetRequiredSkillsAsync(_org.Hr.Id, _org.Vacant.Id, new RequiredSkillsRequest(
        [
            null!,
            new RequiredSkillItem(_org.OtherSkill.Id, "Advanced", "High"),
            new RequiredSkillItem(_org.Forklift.Id, "5", "Urgent"),
            new RequiredSkillItem(_org.Forklift.Id, null!, "Low"),
        ]));

        Assert.Equal(
            ["requiredSkills[0]", "requiredSkills[1].skillId", "requiredSkills[2].criticality", "requiredSkills[2].expectedLevel", "requiredSkills[3].expectedLevel"],
            result.Errors.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(0, _org.Store.SaveCount);
    }

    [Fact]
    public async Task A_skill_is_required_only_once()
    {
        var result = await _service.SetRequiredSkillsAsync(_org.Hr.Id, _org.Vacant.Id, new RequiredSkillsRequest(
        [
            new RequiredSkillItem(_org.Forklift.Id, "Advanced", "High"),
            new RequiredSkillItem(_org.Forklift.Id, "Beginner", "Low"),
        ]));

        Assert.Equal(["Each skill can be required only once."], result.Errors["requiredSkills"]);
        Assert.Empty(_org.Vacant.RequiredSkills);
    }

    [Fact]
    public async Task Vacant_job_is_deleted_and_held_job_is_kept()
    {
        Assert.Equal(ServiceStatus.Ok, (await _service.DeleteAsync(_org.Hr.Id, _org.Vacant.Id)).Status);
        Assert.Equal(ServiceStatus.Conflict, (await _service.DeleteAsync(_org.Hr.Id, _org.TruckDriver.Id)).Status);
        Assert.DoesNotContain(_org.Vacant, _org.Store.Jobs);
        Assert.Contains(_org.TruckDriver, _org.Store.Jobs);
    }

    [Fact]
    public async Task Missing_request_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(_org.Hr.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_org.Hr.Id, _org.Vacant.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SetRequiredSkillsAsync(_org.Hr.Id, _org.Vacant.Id, null!));
    }
}

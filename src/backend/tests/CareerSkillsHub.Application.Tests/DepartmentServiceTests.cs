namespace CareerSkillsHub.Application.Tests;

public sealed class DepartmentServiceTests
{
    private readonly TestOrganisation _org = new();
    private readonly DepartmentService _service;

    public DepartmentServiceTests()
    {
        _service = new DepartmentService(_org.Actors, _org.Store);
    }

    [Fact]
    public async Task Every_role_lists_the_departments_of_its_company_by_name()
    {
        foreach (var user in new[] { _org.Hr, _org.Manager, _org.Employee })
        {
            var result = await _service.ListAsync(user.Id);

            Assert.Equal(["Quality", "Transport", "Warehouse"], result.Value!.Select(d => d.Name));
        }
    }

    [Fact]
    public async Task Unknown_user_is_unauthorized()
    {
        var request = new DepartmentRequest("Finance");

        Assert.Equal(ServiceStatus.Unauthorized, (await _service.ListAsync(Guid.NewGuid())).Status);
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.CreateAsync(Guid.NewGuid(), request)).Status);
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.UpdateAsync(Guid.NewGuid(), _org.Unused.Id, request)).Status);
        Assert.Equal(ServiceStatus.Unauthorized, (await _service.DeleteAsync(Guid.NewGuid(), _org.Unused.Id)).Status);
    }

    [Fact]
    public async Task Managers_and_employees_cannot_change_departments()
    {
        var request = new DepartmentRequest("Finance");
        foreach (var user in _org.NonHrUsers)
        {
            var results = new[]
            {
                (await _service.CreateAsync(user.Id, request)).Status,
                (await _service.UpdateAsync(user.Id, _org.Unused.Id, request)).Status,
                (await _service.DeleteAsync(user.Id, _org.Unused.Id)).Status,
            };

            Assert.All(results, status => Assert.Equal(ServiceStatus.Forbidden, status));
        }

        Assert.Equal(0, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Hr_creates_a_department()
    {
        var result = await _service.CreateAsync(_org.Hr.Id, new DepartmentRequest(" Finance "));

        Assert.Equal("Finance", result.Value!.Name);
        Assert.Contains(_org.Store.Departments, d => d.Id == result.Value.Id && d.CompanyId == _org.Company.Id);
        Assert.Equal(1, _org.Store.SaveCount);
    }

    [Theory]
    [InlineData(" ", "Name is required.")]
    [InlineData("warehouse", "A department already has this name.")]
    public async Task Invalid_name_is_refused(string name, string message)
    {
        var result = await _service.CreateAsync(_org.Hr.Id, new DepartmentRequest(name));

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal([message], result.Errors["name"]);
        Assert.Equal(0, _org.Store.SaveCount);
    }

    [Fact]
    public async Task Name_of_another_company_is_free()
    {
        var result = await _service.CreateAsync(_org.Hr.Id, new DepartmentRequest("Kitchen"));

        Assert.Equal(ServiceStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Hr_renames_a_department_and_may_keep_its_name()
    {
        var renamed = await _service.UpdateAsync(_org.Hr.Id, _org.Warehouse.Id, new DepartmentRequest("Main warehouse"));
        var unchanged = await _service.UpdateAsync(_org.Hr.Id, _org.Transport.Id, new DepartmentRequest("TRANSPORT"));

        Assert.Equal(new DepartmentResponse(_org.Warehouse.Id, "Main warehouse"), renamed.Value);
        Assert.Equal("TRANSPORT", unchanged.Value!.Name);
    }

    [Fact]
    public async Task Rename_to_a_taken_name_is_refused()
    {
        var result = await _service.UpdateAsync(_org.Hr.Id, _org.Warehouse.Id, new DepartmentRequest("Transport"));

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal("Warehouse", _org.Warehouse.Name);
    }

    [Fact]
    public async Task Departments_of_another_company_are_not_found()
    {
        var request = new DepartmentRequest("Taken over");

        Assert.Equal(ServiceStatus.NotFound, (await _service.UpdateAsync(_org.Hr.Id, _org.OtherDepartment.Id, request)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.DeleteAsync(_org.Hr.Id, _org.OtherDepartment.Id)).Status);
        Assert.Equal("Kitchen", _org.OtherDepartment.Name);
    }

    [Fact]
    public async Task Unused_department_is_deleted()
    {
        var result = await _service.DeleteAsync(_org.Hr.Id, _org.Unused.Id);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.DoesNotContain(_org.Unused, _org.Store.Departments);
    }

    [Fact]
    public async Task Department_in_use_is_kept()
    {
        var result = await _service.DeleteAsync(_org.Hr.Id, _org.Warehouse.Id);

        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.Contains(_org.Warehouse, _org.Store.Departments);
    }

    [Fact]
    public async Task Missing_request_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(_org.Hr.Id, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_org.Hr.Id, _org.Unused.Id, null!));
    }
}

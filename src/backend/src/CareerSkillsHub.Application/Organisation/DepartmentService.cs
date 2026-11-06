namespace CareerSkillsHub.Application.Organisation;

public sealed class DepartmentService(ActorResolver actors, IOrganisationRepository organisation)
{
    private const string NotFound = "Department not found.";

    /// <summary>Every role can read the departments of its company.</summary>
    public async Task<ServiceResult<IReadOnlyList<DepartmentResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<IReadOnlyList<DepartmentResponse>>(userId, manageOrganisation: false, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var departments = await organisation.ListDepartmentsAsync(actor!.CompanyId, cancellationToken);

        return ServiceResult<IReadOnlyList<DepartmentResponse>>.Ok([.. departments.OrderBy(d => d.Name).Select(ToResponse)]);
    }

    public async Task<ServiceResult<DepartmentResponse>> CreateAsync(
        Guid userId,
        DepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<DepartmentResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var departments = await organisation.ListDepartmentsAsync(actor!.CompanyId, cancellationToken);
        var errors = Validate(request, departments, exceptId: null, out var name);
        if (errors.Count > 0)
        {
            return ServiceResult<DepartmentResponse>.Invalid(errors);
        }

        var department = new Department(actor.CompanyId, name!);
        organisation.Add(department);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<DepartmentResponse>.Ok(ToResponse(department));
    }

    public async Task<ServiceResult<DepartmentResponse>> UpdateAsync(
        Guid userId,
        Guid departmentId,
        DepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<DepartmentResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var departments = await organisation.ListDepartmentsAsync(actor!.CompanyId, cancellationToken);
        if (departments.SingleOrDefault(d => d.Id == departmentId) is not { } department)
        {
            return ServiceResult<DepartmentResponse>.NotFound(NotFound);
        }

        var errors = Validate(request, departments, departmentId, out var name);
        if (errors.Count > 0)
        {
            return ServiceResult<DepartmentResponse>.Invalid(errors);
        }

        department.Rename(name!);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<DepartmentResponse>.Ok(ToResponse(department));
    }

    public async Task<ServiceResult<Guid>> DeleteAsync(Guid userId, Guid departmentId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<Guid>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var departments = await organisation.ListDepartmentsAsync(actor!.CompanyId, cancellationToken);
        if (departments.SingleOrDefault(d => d.Id == departmentId) is not { } department)
        {
            return ServiceResult<Guid>.NotFound(NotFound);
        }

        if (await organisation.IsDepartmentUsedAsync(departmentId, cancellationToken))
        {
            return ServiceResult<Guid>.Conflict("This department still has jobs or employees, now or in their job history.");
        }

        organisation.Remove(department);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<Guid>.Ok(departmentId);
    }

    private static FieldErrors Validate(DepartmentRequest request, IReadOnlyList<Department> departments, Guid? exceptId, out string? name)
    {
        var errors = new FieldErrors();
        name = errors.Required("name", request.Name, Department.NameMaxLength, "Name");
        var taken = name;
        if (taken is not null && departments.Any(d => d.Id != exceptId && string.Equals(d.Name, taken, StringComparison.OrdinalIgnoreCase)))
        {
            errors["name"] = ["A department already has this name."];
        }

        return errors;
    }

    private static DepartmentResponse ToResponse(Department department) => new(department.Id, department.Name);
}

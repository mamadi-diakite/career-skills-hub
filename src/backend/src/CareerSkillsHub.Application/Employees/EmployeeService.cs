namespace CareerSkillsHub.Application.Employees;

/// <summary>Employee records: HR sees and manages everyone, a manager sees their team, an employee sees themself.</summary>
public sealed class EmployeeService(
    ActorResolver actors,
    IEmployeeRepository employees,
    IOrganisationRepository organisation,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
{
    private const string NotFound = "Employee not found.";

    /// <summary>The employees the caller is allowed to see, sorted by last name.</summary>
    public async Task<ServiceResult<IReadOnlyList<EmployeeSummaryResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<IReadOnlyList<EmployeeSummaryResponse>>(userId, manageOrganisation: false, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var context = await LoadAsync(actor!.CompanyId, cancellationToken);
        var visible = context.Employees.Values
            .Where(e => AccessRules.CanSeeEmployee(actor, e))
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(context.ToSummary);

        return ServiceResult<IReadOnlyList<EmployeeSummaryResponse>>.Ok([.. visible]);
    }

    public async Task<ServiceResult<EmployeeDetailsResponse>> GetAsync(Guid userId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<EmployeeDetailsResponse>(userId, manageOrganisation: false, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (await employees.FindAsync(actor!.CompanyId, employeeId, cancellationToken) is not { } employee)
        {
            return ServiceResult<EmployeeDetailsResponse>.NotFound(NotFound);
        }

        if (!AccessRules.CanSeeEmployee(actor, employee))
        {
            return ServiceResult<EmployeeDetailsResponse>.Forbidden(AccessRules.EmployeeIsNotVisible);
        }

        var context = await LoadAsync(actor.CompanyId, cancellationToken);

        return ServiceResult<EmployeeDetailsResponse>.Ok(context.ToDetails(employee));
    }

    public async Task<ServiceResult<EmployeeDetailsResponse>> CreateAsync(
        Guid userId,
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<EmployeeDetailsResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var context = await LoadAsync(actor!.CompanyId, cancellationToken);
        var errors = new FieldErrors();
        var firstName = errors.Required("firstName", request.FirstName, Employee.NameMaxLength, "First name");
        var lastName = errors.Required("lastName", request.LastName, Employee.NameMaxLength, "Last name");

        string? email = null;
        if (!Employee.IsValidEmail(request.Email))
        {
            errors["email"] = ["Enter a valid email address."];
        }
        else
        {
            email = request.Email.Trim().ToLowerInvariant();
            if (context.Employees.Values.Any(e => e.Email == email))
            {
                errors["email"] = ["An employee already has this email."];
            }
        }

        if (request.HireDate is null)
        {
            errors["hireDate"] = ["Hire date is required."];
        }

        AddJobErrors(errors, context, request.JobId, request.DepartmentId);
        if (request.ManagerId is { } managerId && !context.Employees.ContainsKey(managerId))
        {
            errors["managerId"] = ["Choose a manager among the employees."];
        }

        if (errors.Count > 0)
        {
            return ServiceResult<EmployeeDetailsResponse>.Invalid(errors);
        }

        var employee = new Employee(
            actor.CompanyId,
            firstName!,
            lastName!,
            email!,
            request.HireDate!.Value,
            request.JobId,
            request.DepartmentId,
            request.ManagerId,
            timeProvider.GetUtcNow().UtcDateTime);
        employees.Add(employee);
        await employees.SaveChangesAsync(cancellationToken);

        return ServiceResult<EmployeeDetailsResponse>.Ok(context.ToDetails(employee));
    }

    /// <summary>Moves the employee to another job or department; the previous job stays in the history.</summary>
    public async Task<ServiceResult<EmployeeDetailsResponse>> ChangeJobAsync(
        Guid userId,
        Guid employeeId,
        JobChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<EmployeeDetailsResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (await employees.FindAsync(actor!.CompanyId, employeeId, cancellationToken) is not { } employee)
        {
            return ServiceResult<EmployeeDetailsResponse>.NotFound(NotFound);
        }

        var context = await LoadAsync(actor.CompanyId, cancellationToken);
        var errors = new FieldErrors();
        AddJobErrors(errors, context, request.JobId, request.DepartmentId);
        if (request.JobId == employee.JobId && request.DepartmentId == employee.DepartmentId)
        {
            errors["jobId"] = ["Choose another job or department than the current one."];
        }

        var currentStart = employee.JobHistory[^1].StartDate;
        if (request.EffectiveDate is not { } effectiveDate)
        {
            errors["effectiveDate"] = ["Effective date is required."];
        }
        else if (effectiveDate <= currentStart)
        {
            errors["effectiveDate"] = [$"The change must start after {currentStart:yyyy-MM-dd}, when the current job started."];
        }

        if (errors.Count > 0)
        {
            return ServiceResult<EmployeeDetailsResponse>.Invalid(errors);
        }

        employee.ChangeJob(request.JobId, request.DepartmentId, request.EffectiveDate!.Value);
        await employees.SaveChangesAsync(cancellationToken);

        return ServiceResult<EmployeeDetailsResponse>.Ok(context.ToDetails(employee));
    }

    /// <summary>Opens an account for the employee with a temporary password that HR hands over.</summary>
    public async Task<ServiceResult<InvitationResponse>> InviteAsync(
        Guid userId,
        Guid employeeId,
        InvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<InvitationResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (await employees.FindAsync(actor!.CompanyId, employeeId, cancellationToken) is not { } employee)
        {
            return ServiceResult<InvitationResponse>.NotFound(NotFound);
        }

        if (!EnumValue.TryParse<UserRole>(request.Role, out var role))
        {
            return ServiceResult<InvitationResponse>.Invalid(new FieldErrors { ["role"] = ["Choose HrAdmin, Manager or Employee."] });
        }

        var accounts = await users.ListByCompanyAsync(actor.CompanyId, cancellationToken);
        if (accounts.Any(u => u.EmployeeId == employee.Id))
        {
            return ServiceResult<InvitationResponse>.Conflict("This employee already has an account.");
        }

        if (await users.FindByEmailAsync(employee.Email, cancellationToken) is not null)
        {
            return ServiceResult<InvitationResponse>.Conflict("An account already uses the email of this employee.");
        }

        var password = TemporaryPassword.Create();
        var user = new User(
            actor.CompanyId,
            employee.Email,
            employee.FullName,
            passwordHasher.Hash(password),
            role,
            employee.Id,
            timeProvider.GetUtcNow().UtcDateTime);
        users.Add(user);
        await users.SaveChangesAsync(cancellationToken);

        return ServiceResult<InvitationResponse>.Ok(new InvitationResponse(employee.Id, user.Email, role.ToString(), password));
    }

    private static void AddJobErrors(FieldErrors errors, EmployeeContext context, Guid jobId, Guid departmentId)
    {
        if (!context.Jobs.ContainsKey(jobId))
        {
            errors["jobId"] = ["Choose a job."];
        }

        if (!context.Departments.ContainsKey(departmentId))
        {
            errors["departmentId"] = ["Choose a department."];
        }
    }

    private async Task<EmployeeContext> LoadAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var all = await employees.ListAsync(companyId, cancellationToken);
        var jobs = await organisation.ListJobsAsync(companyId, cancellationToken);
        var departments = await organisation.ListDepartmentsAsync(companyId, cancellationToken);
        var accounts = await users.ListByCompanyAsync(companyId, cancellationToken);

        return new EmployeeContext(
            all.ToDictionary(e => e.Id),
            jobs.ToDictionary(j => j.Id),
            departments.ToDictionary(d => d.Id),
            accounts.Where(u => u.EmployeeId is not null).ToDictionary(u => u.EmployeeId!.Value, u => u.Role));
    }

    private sealed record EmployeeContext(
        IReadOnlyDictionary<Guid, Employee> Employees,
        IReadOnlyDictionary<Guid, Job> Jobs,
        IReadOnlyDictionary<Guid, Department> Departments,
        IReadOnlyDictionary<Guid, UserRole> AccountRoles)
    {
        // Jobs, departments and employees in use cannot be deleted, so the lookups always succeed.
        public EmployeeSummaryResponse ToSummary(Employee employee) =>
            new(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                employee.Email,
                employee.HireDate,
                employee.JobId,
                Jobs[employee.JobId].Title,
                employee.DepartmentId,
                Departments[employee.DepartmentId].Name,
                employee.ManagerId,
                employee.ManagerId is { } managerId ? Employees[managerId].FullName : null,
                AccountRoles.TryGetValue(employee.Id, out var role) ? role.ToString() : null);

        public EmployeeDetailsResponse ToDetails(Employee employee) =>
            new(
                ToSummary(employee),
                [.. employee.JobHistory
                    .Reverse()
                    .Select(a => new JobAssignmentResponse(
                        a.JobId,
                        Jobs[a.JobId].Title,
                        a.DepartmentId,
                        Departments[a.DepartmentId].Name,
                        a.StartDate,
                        a.EndDate))]);
    }
}

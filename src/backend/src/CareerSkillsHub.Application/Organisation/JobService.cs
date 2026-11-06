namespace CareerSkillsHub.Application.Organisation;

/// <summary>Job descriptions and the skills each job requires.</summary>
public sealed class JobService(ActorResolver actors, IOrganisationRepository organisation)
{
    private const string NotFound = "Job not found.";

    /// <summary>Every role can read the jobs of its company.</summary>
    public async Task<ServiceResult<IReadOnlyList<JobResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<IReadOnlyList<JobResponse>>(userId, manageOrganisation: false, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var context = await LoadAsync(actor!.CompanyId, cancellationToken);

        return ServiceResult<IReadOnlyList<JobResponse>>.Ok([.. context.Jobs.OrderBy(j => j.Title).Select(context.ToResponse)]);
    }

    public async Task<ServiceResult<JobResponse>> GetAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<JobResponse>(userId, manageOrganisation: false, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var context = await LoadAsync(actor!.CompanyId, cancellationToken);

        return context.Jobs.SingleOrDefault(j => j.Id == jobId) is { } job
            ? ServiceResult<JobResponse>.Ok(context.ToResponse(job))
            : ServiceResult<JobResponse>.NotFound(NotFound);
    }

    public async Task<ServiceResult<JobResponse>> CreateAsync(Guid userId, JobRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<JobResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var context = await LoadAsync(actor!.CompanyId, cancellationToken);
        var errors = Validate(request, context, exceptId: null, out var title, out var description);
        if (errors.Count > 0)
        {
            return ServiceResult<JobResponse>.Invalid(errors);
        }

        var job = new Job(actor.CompanyId, request.DepartmentId, title!, description);
        organisation.Add(job);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<JobResponse>.Ok(context.ToResponse(job));
    }

    public async Task<ServiceResult<JobResponse>> UpdateAsync(
        Guid userId,
        Guid jobId,
        JobRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<JobResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var context = await LoadAsync(actor!.CompanyId, cancellationToken);
        if (context.Jobs.SingleOrDefault(j => j.Id == jobId) is not { } job)
        {
            return ServiceResult<JobResponse>.NotFound(NotFound);
        }

        var errors = Validate(request, context, jobId, out var title, out var description);
        if (errors.Count > 0)
        {
            return ServiceResult<JobResponse>.Invalid(errors);
        }

        job.Update(request.DepartmentId, title!, description);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<JobResponse>.Ok(context.ToResponse(job));
    }

    public async Task<ServiceResult<JobResponse>> SetRequiredSkillsAsync(
        Guid userId,
        Guid jobId,
        RequiredSkillsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<JobResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var context = await LoadAsync(actor!.CompanyId, cancellationToken);
        if (context.Jobs.SingleOrDefault(j => j.Id == jobId) is not { } job)
        {
            return ServiceResult<JobResponse>.NotFound(NotFound);
        }

        var errors = new FieldErrors();
        var requiredSkills = new List<RequiredSkill>();
        var items = request.RequiredSkills ?? [];
        for (var i = 0; i < items.Count; i++)
        {
            var field = $"requiredSkills[{i}]";
            var item = items[i];
            if (item is null)
            {
                errors[field] = ["Choose a skill, a level and a criticality."];
                continue;
            }

            var skillKnown = context.Skills.ContainsKey(item.SkillId);
            if (!skillKnown)
            {
                errors[$"{field}.skillId"] = ["Choose a skill of the framework."];
            }

            var levelKnown = EnumValue.TryParse<SkillLevel>(item.ExpectedLevel, out var level);
            if (!levelKnown)
            {
                errors[$"{field}.expectedLevel"] = ["Choose Beginner, Intermediate, Advanced or Expert."];
            }

            var criticalityKnown = EnumValue.TryParse<Criticality>(item.Criticality, out var criticality);
            if (!criticalityKnown)
            {
                errors[$"{field}.criticality"] = ["Choose Low, Medium or High."];
            }

            if (skillKnown && levelKnown && criticalityKnown)
            {
                requiredSkills.Add(new RequiredSkill(item.SkillId, level, criticality));
            }
        }

        if (requiredSkills.GroupBy(r => r.SkillId).Any(g => g.Count() > 1))
        {
            errors["requiredSkills"] = ["Each skill can be required only once."];
        }

        if (errors.Count > 0)
        {
            return ServiceResult<JobResponse>.Invalid(errors);
        }

        job.SetRequiredSkills(requiredSkills);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<JobResponse>.Ok(context.ToResponse(job));
    }

    public async Task<ServiceResult<Guid>> DeleteAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<Guid>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var jobs = await organisation.ListJobsAsync(actor!.CompanyId, cancellationToken);
        if (jobs.SingleOrDefault(j => j.Id == jobId) is not { } job)
        {
            return ServiceResult<Guid>.NotFound(NotFound);
        }

        if (await organisation.IsJobUsedAsync(jobId, cancellationToken))
        {
            return ServiceResult<Guid>.Conflict("This job is or was held by employees.");
        }

        organisation.Remove(job);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<Guid>.Ok(jobId);
    }

    private static FieldErrors Validate(
        JobRequest request,
        JobContext context,
        Guid? exceptId,
        out string? title,
        out string? description)
    {
        var errors = new FieldErrors();
        title = errors.Required("title", request.Title, Job.TitleMaxLength, "Title");
        var taken = title;
        if (taken is not null && context.Jobs.Any(j => j.Id != exceptId && string.Equals(j.Title, taken, StringComparison.OrdinalIgnoreCase)))
        {
            errors["title"] = ["A job already has this title."];
        }

        if (!context.Departments.ContainsKey(request.DepartmentId))
        {
            errors["departmentId"] = ["Choose a department."];
        }

        description = errors.Optional("description", request.Description, Job.DescriptionMaxLength);

        return errors;
    }

    private async Task<JobContext> LoadAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var jobs = await organisation.ListJobsAsync(companyId, cancellationToken);
        var departments = await organisation.ListDepartmentsAsync(companyId, cancellationToken);
        var skills = await organisation.ListSkillsAsync(companyId, cancellationToken);

        return new JobContext(jobs, departments.ToDictionary(d => d.Id), skills.ToDictionary(s => s.Id));
    }

    private sealed record JobContext(
        IReadOnlyList<Job> Jobs,
        IReadOnlyDictionary<Guid, Department> Departments,
        IReadOnlyDictionary<Guid, Skill> Skills)
    {
        // Departments and skills cannot be deleted while a job uses them, so the lookups always succeed.
        public JobResponse ToResponse(Job job) =>
            new(
                job.Id,
                job.Title,
                job.DepartmentId,
                Departments[job.DepartmentId].Name,
                job.Description,
                [.. job.RequiredSkills
                    .OrderByDescending(r => r.Criticality)
                    .ThenBy(r => Skills[r.SkillId].Name)
                    .Select(r => new RequiredSkillResponse(r.SkillId, Skills[r.SkillId].Name, r.ExpectedLevel.ToString(), r.Criticality.ToString()))]);
    }
}

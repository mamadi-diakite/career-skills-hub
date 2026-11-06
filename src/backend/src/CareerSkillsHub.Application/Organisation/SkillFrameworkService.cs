namespace CareerSkillsHub.Application.Organisation;

/// <summary>Skill domains and the skills they group, with what each of the four levels means.</summary>
public sealed class SkillFrameworkService(ActorResolver actors, IOrganisationRepository organisation)
{
    private const string DomainNotFound = "Skill domain not found.";
    private const string SkillNotFound = "Skill not found.";

    /// <summary>Every role can read the framework of its company.</summary>
    public async Task<ServiceResult<IReadOnlyList<SkillDomainResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<IReadOnlyList<SkillDomainResponse>>(userId, manageOrganisation: false, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var domains = await organisation.ListSkillDomainsAsync(actor!.CompanyId, cancellationToken);
        var skills = await organisation.ListSkillsAsync(actor.CompanyId, cancellationToken);

        return ServiceResult<IReadOnlyList<SkillDomainResponse>>.Ok([.. domains.OrderBy(d => d.Name).Select(d => ToResponse(d, skills))]);
    }

    public async Task<ServiceResult<SkillDomainResponse>> CreateDomainAsync(
        Guid userId,
        SkillDomainRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<SkillDomainResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var domains = await organisation.ListSkillDomainsAsync(actor!.CompanyId, cancellationToken);
        var errors = ValidateDomain(request, domains, exceptId: null, out var name, out var description);
        if (errors.Count > 0)
        {
            return ServiceResult<SkillDomainResponse>.Invalid(errors);
        }

        var domain = new SkillDomain(actor.CompanyId, name!, description);
        organisation.Add(domain);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<SkillDomainResponse>.Ok(ToResponse(domain, []));
    }

    public async Task<ServiceResult<SkillDomainResponse>> UpdateDomainAsync(
        Guid userId,
        Guid domainId,
        SkillDomainRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<SkillDomainResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var domains = await organisation.ListSkillDomainsAsync(actor!.CompanyId, cancellationToken);
        if (domains.SingleOrDefault(d => d.Id == domainId) is not { } domain)
        {
            return ServiceResult<SkillDomainResponse>.NotFound(DomainNotFound);
        }

        var errors = ValidateDomain(request, domains, domainId, out var name, out var description);
        if (errors.Count > 0)
        {
            return ServiceResult<SkillDomainResponse>.Invalid(errors);
        }

        domain.Update(name!, description);
        await organisation.SaveChangesAsync(cancellationToken);
        var skills = await organisation.ListSkillsAsync(actor.CompanyId, cancellationToken);

        return ServiceResult<SkillDomainResponse>.Ok(ToResponse(domain, skills));
    }

    public async Task<ServiceResult<Guid>> DeleteDomainAsync(Guid userId, Guid domainId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<Guid>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var domains = await organisation.ListSkillDomainsAsync(actor!.CompanyId, cancellationToken);
        if (domains.SingleOrDefault(d => d.Id == domainId) is not { } domain)
        {
            return ServiceResult<Guid>.NotFound(DomainNotFound);
        }

        var skills = await organisation.ListSkillsAsync(actor.CompanyId, cancellationToken);
        if (skills.Any(s => s.DomainId == domainId))
        {
            return ServiceResult<Guid>.Conflict("Move or delete the skills of this domain first.");
        }

        organisation.Remove(domain);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<Guid>.Ok(domainId);
    }

    public async Task<ServiceResult<SkillResponse>> CreateSkillAsync(
        Guid userId,
        SkillRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<SkillResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var skills = await organisation.ListSkillsAsync(actor!.CompanyId, cancellationToken);
        var (errors, values) = await ValidateSkillAsync(actor.CompanyId, request, skills, exceptId: null, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<SkillResponse>.Invalid(errors);
        }

        var skill = new Skill(actor.CompanyId, request.DomainId, values.Name, values.Description, values.Levels);
        organisation.Add(skill);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<SkillResponse>.Ok(ToResponse(skill));
    }

    public async Task<ServiceResult<SkillResponse>> UpdateSkillAsync(
        Guid userId,
        Guid skillId,
        SkillRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (actor, failure) = await actors.RequireAsync<SkillResponse>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var skills = await organisation.ListSkillsAsync(actor!.CompanyId, cancellationToken);
        if (skills.SingleOrDefault(s => s.Id == skillId) is not { } skill)
        {
            return ServiceResult<SkillResponse>.NotFound(SkillNotFound);
        }

        var (errors, values) = await ValidateSkillAsync(actor.CompanyId, request, skills, skillId, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<SkillResponse>.Invalid(errors);
        }

        skill.Update(request.DomainId, values.Name, values.Description, values.Levels);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<SkillResponse>.Ok(ToResponse(skill));
    }

    public async Task<ServiceResult<Guid>> DeleteSkillAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default)
    {
        var (actor, failure) = await actors.RequireAsync<Guid>(userId, manageOrganisation: true, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var skills = await organisation.ListSkillsAsync(actor!.CompanyId, cancellationToken);
        if (skills.SingleOrDefault(s => s.Id == skillId) is not { } skill)
        {
            return ServiceResult<Guid>.NotFound(SkillNotFound);
        }

        var jobs = await organisation.ListJobsAsync(actor.CompanyId, cancellationToken);
        if (jobs.Any(j => j.RequiredSkills.Any(r => r.SkillId == skillId)))
        {
            return ServiceResult<Guid>.Conflict("This skill is required by a job. Remove it from the job first.");
        }

        organisation.Remove(skill);
        await organisation.SaveChangesAsync(cancellationToken);

        return ServiceResult<Guid>.Ok(skillId);
    }

    internal static SkillResponse ToResponse(Skill skill) =>
        new(
            skill.Id,
            skill.DomainId,
            skill.Name,
            skill.Description,
            new SkillLevelTexts(skill.Levels.Beginner, skill.Levels.Intermediate, skill.Levels.Advanced, skill.Levels.Expert));

    private static SkillDomainResponse ToResponse(SkillDomain domain, IReadOnlyList<Skill> skills) =>
        new(
            domain.Id,
            domain.Name,
            domain.Description,
            [.. skills.Where(s => s.DomainId == domain.Id).OrderBy(s => s.Name).Select(ToResponse)]);

    private static FieldErrors ValidateDomain(
        SkillDomainRequest request,
        IReadOnlyList<SkillDomain> domains,
        Guid? exceptId,
        out string? name,
        out string? description)
    {
        var errors = new FieldErrors();
        name = errors.Required("name", request.Name, SkillDomain.NameMaxLength, "Name");
        description = errors.Optional("description", request.Description, SkillDomain.DescriptionMaxLength);
        var taken = name;
        if (taken is not null && domains.Any(d => d.Id != exceptId && string.Equals(d.Name, taken, StringComparison.OrdinalIgnoreCase)))
        {
            errors["name"] = ["A skill domain already has this name."];
        }

        return errors;
    }

    private async Task<(FieldErrors Errors, SkillValues Values)> ValidateSkillAsync(
        Guid companyId,
        SkillRequest request,
        IReadOnlyList<Skill> skills,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var errors = new FieldErrors();
        var domains = await organisation.ListSkillDomainsAsync(companyId, cancellationToken);
        if (!domains.Any(d => d.Id == request.DomainId))
        {
            errors["domainId"] = ["Choose a skill domain."];
        }

        var name = errors.Required("name", request.Name, Skill.NameMaxLength, "Name");
        if (name is not null && skills.Any(s => s.Id != exceptId && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            errors["name"] = ["A skill already has this name."];
        }

        var description = errors.Optional("description", request.Description, Skill.DescriptionMaxLength);
        var levels = request.Levels;
        var beginner = errors.Required("levels.beginner", levels?.Beginner, Skill.LevelDescriptionMaxLength, "The Beginner level");
        var intermediate = errors.Required("levels.intermediate", levels?.Intermediate, Skill.LevelDescriptionMaxLength, "The Intermediate level");
        var advanced = errors.Required("levels.advanced", levels?.Advanced, Skill.LevelDescriptionMaxLength, "The Advanced level");
        var expert = errors.Required("levels.expert", levels?.Expert, Skill.LevelDescriptionMaxLength, "The Expert level");

        return (errors, new SkillValues(name!, description, new SkillLevelDescriptions(beginner!, intermediate!, advanced!, expert!)));
    }

    private sealed record SkillValues(string Name, string? Description, SkillLevelDescriptions Levels);
}

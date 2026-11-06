namespace CareerSkillsHub.Application.Companies;

public sealed class CompanyService(ActorResolver actors, IUserRepository users)
{
    /// <summary>Every role can read the profile of its own company.</summary>
    public async Task<ServiceResult<CompanyResponse>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await actors.FindAsync(userId, cancellationToken) is not { } actor)
        {
            return ServiceResult<CompanyResponse>.Unauthorized();
        }

        return ServiceResult<CompanyResponse>.Ok(ToResponse(await CompanyOfAsync(actor, cancellationToken)));
    }

    public async Task<ServiceResult<CompanyResponse>> UpdateAsync(
        Guid userId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await actors.FindAsync(userId, cancellationToken) is not { } actor)
        {
            return ServiceResult<CompanyResponse>.Unauthorized();
        }

        if (!AccessRules.CanManageOrganisation(actor))
        {
            return ServiceResult<CompanyResponse>.Forbidden("Only HR admins can edit the company profile.");
        }

        var errors = new FieldErrors();
        var name = errors.Required("name", request.Name, Company.NameMaxLength, "Company name");
        errors.Range("headcount", request.Headcount, 1, Company.MaxHeadcount, "Headcount");
        if (errors.Count > 0)
        {
            return ServiceResult<CompanyResponse>.Invalid(errors);
        }

        var company = await CompanyOfAsync(actor, cancellationToken);
        company.UpdateProfile(name!, request.Headcount);
        await users.SaveChangesAsync(cancellationToken);

        return ServiceResult<CompanyResponse>.Ok(ToResponse(company));
    }

    // A user cannot exist without its company (foreign key).
    private async Task<Company> CompanyOfAsync(Actor actor, CancellationToken cancellationToken) =>
        (await users.FindCompanyAsync(actor.CompanyId, cancellationToken))!;

    private static CompanyResponse ToResponse(Company company) => new(company.Id, company.Name, company.Headcount);
}

namespace CareerSkillsHub.Domain;

/// <summary>A group of related skills, for example "Safety".</summary>
public sealed class SkillDomain
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private SkillDomain()
    {
    }

    public SkillDomain(Guid companyId, string name, string? description)
    {
        Id = Guid.CreateVersion7();
        CompanyId = Guard.NotEmpty(companyId, nameof(companyId));
        Name = Guard.Required(name, nameof(name), NameMaxLength);
        Description = Guard.Optional(description, nameof(description), DescriptionMaxLength);
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }
}

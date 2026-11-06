namespace CareerSkillsHub.Domain;

/// <summary>A know-how in a skill domain, with what each of the four levels means for it.</summary>
public sealed class Skill
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int LevelDescriptionMaxLength = 300;

    private Skill()
    {
    }

    public Skill(Guid companyId, Guid domainId, string name, string? description, SkillLevelDescriptions levels)
    {
        ArgumentNullException.ThrowIfNull(levels);

        Id = Guid.CreateVersion7();
        CompanyId = Guard.NotEmpty(companyId, nameof(companyId));
        DomainId = Guard.NotEmpty(domainId, nameof(domainId));
        Name = Guard.Required(name, nameof(name), NameMaxLength);
        Description = Guard.Optional(description, nameof(description), DescriptionMaxLength);
        SetLevels(levels);
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid DomainId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string BeginnerDescription { get; private set; } = string.Empty;

    public string IntermediateDescription { get; private set; } = string.Empty;

    public string AdvancedDescription { get; private set; } = string.Empty;

    public string ExpertDescription { get; private set; } = string.Empty;

    private void SetLevels(SkillLevelDescriptions levels)
    {
        BeginnerDescription = Guard.Required(levels.Beginner, "levels.beginner", LevelDescriptionMaxLength);
        IntermediateDescription = Guard.Required(levels.Intermediate, "levels.intermediate", LevelDescriptionMaxLength);
        AdvancedDescription = Guard.Required(levels.Advanced, "levels.advanced", LevelDescriptionMaxLength);
        ExpertDescription = Guard.Required(levels.Expert, "levels.expert", LevelDescriptionMaxLength);
    }
}

public sealed record SkillLevelDescriptions(string Beginner, string Intermediate, string Advanced, string Expert);

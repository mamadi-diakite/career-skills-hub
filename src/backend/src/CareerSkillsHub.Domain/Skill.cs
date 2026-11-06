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
        Id = Guid.CreateVersion7();
        CompanyId = Guard.NotEmpty(companyId, nameof(companyId));
        Update(domainId, name, description, levels);
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

    public SkillLevelDescriptions Levels =>
        new(BeginnerDescription, IntermediateDescription, AdvancedDescription, ExpertDescription);

    /// <summary>Validates every value before changing any.</summary>
    public void Update(Guid domainId, string name, string? description, SkillLevelDescriptions levels)
    {
        ArgumentNullException.ThrowIfNull(levels);

        var newDomainId = Guard.NotEmpty(domainId, nameof(domainId));
        var newName = Guard.Required(name, nameof(name), NameMaxLength);
        var newDescription = Guard.Optional(description, nameof(description), DescriptionMaxLength);
        var beginner = Guard.Required(levels.Beginner, "levels.beginner", LevelDescriptionMaxLength);
        var intermediate = Guard.Required(levels.Intermediate, "levels.intermediate", LevelDescriptionMaxLength);
        var advanced = Guard.Required(levels.Advanced, "levels.advanced", LevelDescriptionMaxLength);
        var expert = Guard.Required(levels.Expert, "levels.expert", LevelDescriptionMaxLength);

        DomainId = newDomainId;
        Name = newName;
        Description = newDescription;
        BeginnerDescription = beginner;
        IntermediateDescription = intermediate;
        AdvancedDescription = advanced;
        ExpertDescription = expert;
    }
}

public sealed record SkillLevelDescriptions(string Beginner, string Intermediate, string Advanced, string Expert);

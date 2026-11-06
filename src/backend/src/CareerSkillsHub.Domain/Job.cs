namespace CareerSkillsHub.Domain;

/// <summary>A position in the company, for example "Forklift operator".</summary>
public sealed class Job
{
    public const int TitleMaxLength = 150;
    public const int DescriptionMaxLength = 2000;

    private readonly List<JobRequiredSkill> _requiredSkills = [];

    private Job()
    {
    }

    public Job(Guid companyId, Guid departmentId, string title, string? description)
    {
        Id = Guid.CreateVersion7();
        CompanyId = Guard.NotEmpty(companyId, nameof(companyId));
        Update(departmentId, title, description);
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IReadOnlyCollection<JobRequiredSkill> RequiredSkills => _requiredSkills;

    /// <summary>Validates every value before changing any.</summary>
    public void Update(Guid departmentId, string title, string? description)
    {
        var newDepartmentId = Guard.NotEmpty(departmentId, nameof(departmentId));
        var newTitle = Guard.Required(title, nameof(title), TitleMaxLength);
        Description = Guard.Optional(description, nameof(description), DescriptionMaxLength);
        DepartmentId = newDepartmentId;
        Title = newTitle;
    }

    /// <summary>Replaces the required skills, keeping the rows of skills that stay required.</summary>
    public void SetRequiredSkills(IReadOnlyCollection<RequiredSkill> requiredSkills)
    {
        ArgumentNullException.ThrowIfNull(requiredSkills);
        if (requiredSkills.DistinctBy(s => s.SkillId).Count() != requiredSkills.Count)
        {
            throw new ArgumentException("A skill can be required only once.", nameof(requiredSkills));
        }

        foreach (var skill in requiredSkills)
        {
            Guard.NotEmpty(skill.SkillId, nameof(skill.SkillId));
            Guard.Defined(skill.ExpectedLevel, nameof(skill.ExpectedLevel));
            Guard.Defined(skill.Criticality, nameof(skill.Criticality));
        }

        _requiredSkills.RemoveAll(existing => requiredSkills.All(s => s.SkillId != existing.SkillId));
        foreach (var skill in requiredSkills)
        {
            if (_requiredSkills.Find(existing => existing.SkillId == skill.SkillId) is { } existing)
            {
                existing.Update(skill.ExpectedLevel, skill.Criticality);
            }
            else
            {
                _requiredSkills.Add(new JobRequiredSkill(Id, skill.SkillId, skill.ExpectedLevel, skill.Criticality));
            }
        }
    }
}

public sealed record RequiredSkill(Guid SkillId, SkillLevel ExpectedLevel, Criticality Criticality);

/// <summary>A skill a job needs, with the expected level and how critical it is.</summary>
/// <remarks>Created and changed through <see cref="Job.SetRequiredSkills"/>, which validates the values.</remarks>
public sealed class JobRequiredSkill
{
    private JobRequiredSkill()
    {
    }

    internal JobRequiredSkill(Guid jobId, Guid skillId, SkillLevel expectedLevel, Criticality criticality)
    {
        JobId = jobId;
        SkillId = skillId;
        Update(expectedLevel, criticality);
    }

    public Guid JobId { get; private set; }

    public Guid SkillId { get; private set; }

    public SkillLevel ExpectedLevel { get; private set; }

    public Criticality Criticality { get; private set; }

    internal void Update(SkillLevel expectedLevel, Criticality criticality)
    {
        ExpectedLevel = expectedLevel;
        Criticality = criticality;
    }
}

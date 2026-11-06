namespace CareerSkillsHub.Application.Contracts.Organisation;

public sealed record DepartmentResponse(Guid Id, string Name);

public sealed record DepartmentRequest(string Name);

/// <summary>What each of the four levels means for a skill.</summary>
public sealed record SkillLevelTexts(string Beginner, string Intermediate, string Advanced, string Expert);

public sealed record SkillResponse(Guid Id, Guid DomainId, string Name, string? Description, SkillLevelTexts Levels);

/// <param name="Skills">Sorted by name.</param>
public sealed record SkillDomainResponse(Guid Id, string Name, string? Description, IReadOnlyList<SkillResponse> Skills);

public sealed record SkillDomainRequest(string Name, string? Description);

public sealed record SkillRequest(Guid DomainId, string Name, string? Description, SkillLevelTexts? Levels);

/// <param name="ExpectedLevel">Beginner, Intermediate, Advanced or Expert.</param>
/// <param name="Criticality">Low, Medium or High.</param>
public sealed record RequiredSkillResponse(Guid SkillId, string SkillName, string ExpectedLevel, string Criticality);

/// <param name="RequiredSkills">Most critical first, then by skill name.</param>
public sealed record JobResponse(
    Guid Id,
    string Title,
    Guid DepartmentId,
    string DepartmentName,
    string? Description,
    IReadOnlyList<RequiredSkillResponse> RequiredSkills);

public sealed record JobRequest(string Title, Guid DepartmentId, string? Description);

public sealed record RequiredSkillItem(Guid SkillId, string ExpectedLevel, string Criticality);

/// <summary>Replaces all the required skills of a job.</summary>
public sealed record RequiredSkillsRequest(IReadOnlyList<RequiredSkillItem>? RequiredSkills);

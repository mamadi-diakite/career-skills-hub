namespace CareerSkillsHub.Domain;

/// <summary>The four levels a skill is assessed on.</summary>
public enum SkillLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4,
}

/// <summary>How much a job suffers when a required skill is missing.</summary>
public enum Criticality
{
    Low = 0,
    Medium = 1,
    High = 2,
}

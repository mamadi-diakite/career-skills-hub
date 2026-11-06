namespace CareerSkillsHub.Domain;

/// <summary>The employer using the app.</summary>
public sealed class Company
{
    public const int NameMaxLength = 150;
    public const int MaxHeadcount = 1_000_000;

    private Company()
    {
    }

    public Company(string name, int headcount, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Name = Guard.Required(name, nameof(name), NameMaxLength);
        Headcount = Guard.Range(headcount, nameof(headcount), 1, MaxHeadcount);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Declared number of employees, which can differ from the employees recorded in the app.</summary>
    public int Headcount { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void UpdateProfile(string name, int headcount)
    {
        var newName = Guard.Required(name, nameof(name), NameMaxLength);
        Headcount = Guard.Range(headcount, nameof(headcount), 1, MaxHeadcount);
        Name = newName;
    }
}

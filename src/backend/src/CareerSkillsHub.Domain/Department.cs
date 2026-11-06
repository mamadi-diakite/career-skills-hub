namespace CareerSkillsHub.Domain;

/// <summary>A team or service inside the company.</summary>
public sealed class Department
{
    public const int NameMaxLength = 100;

    private Department()
    {
    }

    public Department(Guid companyId, string name)
    {
        Id = Guid.CreateVersion7();
        CompanyId = Guard.NotEmpty(companyId, nameof(companyId));
        Name = Guard.Required(name, nameof(name), NameMaxLength);
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public void Rename(string name) => Name = Guard.Required(name, nameof(name), NameMaxLength);
}

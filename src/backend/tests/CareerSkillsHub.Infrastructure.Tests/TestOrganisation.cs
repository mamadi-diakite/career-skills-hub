namespace CareerSkillsHub.Infrastructure.Tests;

/// <summary>A small fictional company saved in the test database.</summary>
internal sealed class TestOrganisation
{
    public static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    public static readonly SkillLevelDescriptions Levels = new(
        "Knows the basics", "Works alone on usual cases", "Handles complex cases", "Teaches others");

    private TestOrganisation(Company company, Department department, Job job, SkillDomain domain, Skill skill)
    {
        Company = company;
        Department = department;
        Job = job;
        Domain = domain;
        Skill = skill;
    }

    public Company Company { get; }

    public Department Department { get; }

    public Job Job { get; }

    public SkillDomain Domain { get; }

    public Skill Skill { get; }

    public static async Task<TestOrganisation> CreateAsync(SqliteTestDatabase database, string companyName = "Blue Harbour Logistics")
    {
        var company = new Company(companyName, 64, Now);
        var department = new Department(company.Id, "Warehouse");
        var job = new Job(company.Id, department.Id, "Forklift operator", "Moves pallets safely.");
        var domain = new SkillDomain(company.Id, "Safety", null);
        var skill = new Skill(company.Id, domain.Id, "Forklift driving", null, Levels);

        await using var db = database.CreateContext();
        db.AddRange(company, department, job, domain, skill);
        await db.SaveChangesAsync();

        return new TestOrganisation(company, department, job, domain, skill);
    }

    public Employee NewEmployee(string firstName, string email, Guid? managerId = null) =>
        new(Company.Id, firstName, "Martin", email, new DateOnly(2022, 5, 2), Job.Id, Department.Id, managerId, Now);
}

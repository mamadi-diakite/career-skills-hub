namespace CareerSkillsHub.Domain.Tests;

public sealed class OrganisationTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly SkillLevelDescriptions Levels = new(
        "Knows the basics", "Works alone on usual cases", "Handles complex cases", "Teaches others");

    [Fact]
    public void Creates_a_department()
    {
        var department = new Department(CompanyId, " Warehouse ");

        Assert.NotEqual(Guid.Empty, department.Id);
        Assert.Equal(CompanyId, department.CompanyId);
        Assert.Equal("Warehouse", department.Name);
    }

    [Fact]
    public void Department_needs_a_company_and_a_name()
    {
        Assert.Equal("companyId", Assert.Throws<ArgumentException>(() => new Department(Guid.Empty, "Warehouse")).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => new Department(CompanyId, " ")).ParamName);
    }

    [Fact]
    public void Creates_a_skill_domain()
    {
        var domain = new SkillDomain(CompanyId, " Safety ", " Working safely on site ");

        Assert.NotEqual(Guid.Empty, domain.Id);
        Assert.Equal(CompanyId, domain.CompanyId);
        Assert.Equal("Safety", domain.Name);
        Assert.Equal("Working safely on site", domain.Description);
        Assert.Null(new SkillDomain(CompanyId, "Safety", " ").Description);
    }

    [Fact]
    public void Skill_domain_needs_a_company_and_a_name()
    {
        Assert.Equal("companyId", Assert.Throws<ArgumentException>(() => new SkillDomain(Guid.Empty, "Safety", null)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => new SkillDomain(CompanyId, "", null)).ParamName);
    }

    [Fact]
    public void Creates_a_skill_with_its_four_levels()
    {
        var domainId = Guid.NewGuid();

        var skill = new Skill(CompanyId, domainId, " Forklift driving ", " Driving a counterbalance truck ", Levels);

        Assert.NotEqual(Guid.Empty, skill.Id);
        Assert.Equal(CompanyId, skill.CompanyId);
        Assert.Equal(domainId, skill.DomainId);
        Assert.Equal("Forklift driving", skill.Name);
        Assert.Equal("Driving a counterbalance truck", skill.Description);
        Assert.Equal("Knows the basics", skill.BeginnerDescription);
        Assert.Equal("Works alone on usual cases", skill.IntermediateDescription);
        Assert.Equal("Handles complex cases", skill.AdvancedDescription);
        Assert.Equal("Teaches others", skill.ExpertDescription);
    }

    [Fact]
    public void Skill_rejects_invalid_values()
    {
        var domainId = Guid.NewGuid();

        Assert.Equal("companyId", Assert.Throws<ArgumentException>(() => new Skill(Guid.Empty, domainId, "A", null, Levels)).ParamName);
        Assert.Equal("domainId", Assert.Throws<ArgumentException>(() => new Skill(CompanyId, Guid.Empty, "A", null, Levels)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => new Skill(CompanyId, domainId, " ", null, Levels)).ParamName);
        Assert.Throws<ArgumentNullException>(() => new Skill(CompanyId, domainId, "A", null, null!));
    }

    [Theory]
    [InlineData(0, "levels.beginner")]
    [InlineData(1, "levels.intermediate")]
    [InlineData(2, "levels.advanced")]
    [InlineData(3, "levels.expert")]
    public void Every_level_needs_a_description(int blankLevel, string paramName)
    {
        string[] texts = ["One", "Two", "Three", "Four"];
        texts[blankLevel] = " ";

        var error = Assert.Throws<ArgumentException>(() => new Skill(
            CompanyId, Guid.NewGuid(), "A", null, new SkillLevelDescriptions(texts[0], texts[1], texts[2], texts[3])));

        Assert.Equal(paramName, error.ParamName);
    }

    [Fact]
    public void Creates_a_job_in_a_department()
    {
        var departmentId = Guid.NewGuid();

        var job = new Job(CompanyId, departmentId, " Forklift operator ", " Moves pallets ");

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(CompanyId, job.CompanyId);
        Assert.Equal(departmentId, job.DepartmentId);
        Assert.Equal("Forklift operator", job.Title);
        Assert.Equal("Moves pallets", job.Description);
        Assert.Empty(job.RequiredSkills);
    }

    [Fact]
    public void Job_rejects_invalid_values()
    {
        Assert.Equal("companyId", Assert.Throws<ArgumentException>(() => new Job(Guid.Empty, Guid.NewGuid(), "A", null)).ParamName);
        Assert.Equal("departmentId", Assert.Throws<ArgumentException>(() => new Job(CompanyId, Guid.Empty, "A", null)).ParamName);
        Assert.Equal("title", Assert.Throws<ArgumentException>(() => new Job(CompanyId, Guid.NewGuid(), " ", null)).ParamName);
    }

    [Fact]
    public void Required_skills_are_replaced_and_updated_in_place()
    {
        var job = new Job(CompanyId, Guid.NewGuid(), "Forklift operator", null);
        Guid driving = Guid.NewGuid(), safety = Guid.NewGuid(), stock = Guid.NewGuid();
        job.SetRequiredSkills([new(driving, SkillLevel.Advanced, Criticality.High), new(safety, SkillLevel.Intermediate, Criticality.Medium)]);
        var drivingRow = job.RequiredSkills.Single(s => s.SkillId == driving);

        job.SetRequiredSkills([new(driving, SkillLevel.Expert, Criticality.Low), new(stock, SkillLevel.Beginner, Criticality.Low)]);

        Assert.Equal([driving, stock], job.RequiredSkills.Select(s => s.SkillId));
        Assert.Same(drivingRow, job.RequiredSkills.First());
        Assert.Equal(SkillLevel.Expert, drivingRow.ExpectedLevel);
        Assert.Equal(Criticality.Low, drivingRow.Criticality);
        Assert.All(job.RequiredSkills, s => Assert.Equal(job.Id, s.JobId));
    }

    [Fact]
    public void Required_skills_can_be_cleared()
    {
        var job = new Job(CompanyId, Guid.NewGuid(), "Forklift operator", null);
        job.SetRequiredSkills([new(Guid.NewGuid(), SkillLevel.Advanced, Criticality.High)]);

        job.SetRequiredSkills([]);

        Assert.Empty(job.RequiredSkills);
    }

    [Fact]
    public void Invalid_required_skills_leave_the_job_unchanged()
    {
        var job = new Job(CompanyId, Guid.NewGuid(), "Forklift operator", null);
        var skillId = Guid.NewGuid();
        job.SetRequiredSkills([new(skillId, SkillLevel.Advanced, Criticality.High)]);

        Assert.Throws<ArgumentNullException>(() => job.SetRequiredSkills(null!));
        Assert.Throws<ArgumentException>(() => job.SetRequiredSkills(
            [new(skillId, SkillLevel.Beginner, Criticality.Low), new(skillId, SkillLevel.Expert, Criticality.Low)]));
        Assert.Throws<ArgumentException>(() => job.SetRequiredSkills([new(Guid.Empty, SkillLevel.Beginner, Criticality.Low)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.SetRequiredSkills([new(skillId, (SkillLevel)0, Criticality.Low)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.SetRequiredSkills([new(skillId, SkillLevel.Expert, (Criticality)7)]));

        var row = Assert.Single(job.RequiredSkills);
        Assert.Equal(SkillLevel.Advanced, row.ExpectedLevel);
        Assert.Equal(Criticality.High, row.Criticality);
    }
}

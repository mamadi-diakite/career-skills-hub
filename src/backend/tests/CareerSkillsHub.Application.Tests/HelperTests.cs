namespace CareerSkillsHub.Application.Tests;

public sealed class HelperTests
{
    [Theory]
    [InlineData("Advanced", true, SkillLevel.Advanced)]
    [InlineData(" expert ", true, SkillLevel.Expert)]
    [InlineData("2", true, SkillLevel.Intermediate)]
    [InlineData("9", false, default(SkillLevel))]
    [InlineData("Master", false, default(SkillLevel))]
    [InlineData(null, false, default(SkillLevel))]
    public void Enum_values_are_parsed_by_name_ignoring_case(string? value, bool parsed, SkillLevel expected)
    {
        Assert.Equal(parsed, EnumValue.TryParse<SkillLevel>(value, out var level));
        if (parsed)
        {
            Assert.Equal(expected, level);
        }
    }

    [Fact]
    public void Temporary_passwords_meet_the_policy_and_differ()
    {
        var passwords = Enumerable.Range(0, 20).Select(_ => TemporaryPassword.Create()).ToList();

        Assert.All(passwords, p => Assert.Empty(PasswordPolicy.Check(p)));
        Assert.All(passwords, p => Assert.Matches("^[A-Za-z]{6}-[2-9]{4}-[A-Za-z]{6}$", p));
        Assert.Equal(passwords.Count, passwords.Distinct().Count());
    }

    [Fact]
    public void Application_services_are_registered()
    {
        var services = new ServiceCollection().AddApplication();

        foreach (var type in new[] { typeof(DepartmentService), typeof(SkillFrameworkService), typeof(JobService), typeof(EmployeeService) })
        {
            Assert.Contains(services, d => d.ServiceType == type && d.Lifetime == ServiceLifetime.Scoped);
        }
    }
}

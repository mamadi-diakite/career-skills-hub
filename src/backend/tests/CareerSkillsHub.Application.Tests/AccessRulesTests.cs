namespace CareerSkillsHub.Application.Tests;

public sealed class AccessRulesTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ManagerEmployeeId = Guid.NewGuid();

    private static readonly Actor Hr = new(Guid.NewGuid(), CompanyId, UserRole.HrAdmin, null);
    private static readonly Actor Manager = new(Guid.NewGuid(), CompanyId, UserRole.Manager, ManagerEmployeeId);

    [Theory]
    [InlineData(UserRole.HrAdmin, true)]
    [InlineData(UserRole.Manager, false)]
    [InlineData(UserRole.Employee, false)]
    public void Only_hr_manages_the_organisation(UserRole role, bool expected)
    {
        Assert.Equal(expected, AccessRules.CanManageOrganisation(new Actor(Guid.NewGuid(), CompanyId, role, Guid.NewGuid())));
    }

    [Fact]
    public void Hr_sees_every_employee_of_the_company()
    {
        Assert.True(AccessRules.CanSeeEmployee(Hr, NewEmployee(managerId: null)));
        Assert.True(AccessRules.CanSeeEmployee(Hr, NewEmployee(managerId: ManagerEmployeeId)));
    }

    [Fact]
    public void Manager_sees_their_team_and_themselves_only()
    {
        var self = NewEmployee(managerId: null);
        var manager = Manager with { EmployeeId = self.Id };
        var teamMember = NewEmployee(managerId: self.Id);
        var otherTeam = NewEmployee(managerId: Guid.NewGuid());
        var noManager = NewEmployee(managerId: null);

        Assert.True(AccessRules.CanSeeEmployee(manager, self));
        Assert.True(AccessRules.CanSeeEmployee(manager, teamMember));
        Assert.False(AccessRules.CanSeeEmployee(manager, otherTeam));
        Assert.False(AccessRules.CanSeeEmployee(manager, noManager));
    }

    [Fact]
    public void Employee_sees_only_themselves()
    {
        var self = NewEmployee(managerId: ManagerEmployeeId);
        var employee = new Actor(Guid.NewGuid(), CompanyId, UserRole.Employee, self.Id);
        var colleague = NewEmployee(managerId: ManagerEmployeeId);
        var report = NewEmployee(managerId: self.Id);

        Assert.True(AccessRules.CanSeeEmployee(employee, self));
        Assert.False(AccessRules.CanSeeEmployee(employee, colleague));
        Assert.False(AccessRules.CanSeeEmployee(employee, report));
    }

    [Fact]
    public void A_user_without_employee_record_sees_nobody_unless_hr()
    {
        var manager = Manager with { EmployeeId = null };

        Assert.False(AccessRules.CanSeeEmployee(manager, NewEmployee(managerId: null)));
    }

    [Fact]
    public void Nobody_sees_employees_of_another_company()
    {
        var outsider = new Employee(Guid.NewGuid(), "Jade", "Roux", "jade.roux@example.com", new DateOnly(2020, 1, 6),
            Guid.NewGuid(), Guid.NewGuid(), ManagerEmployeeId, TestClock.Now.UtcDateTime);

        Assert.False(AccessRules.CanSeeEmployee(Hr, outsider));
        Assert.False(AccessRules.CanSeeEmployee(Manager, outsider));
    }

    [Fact]
    public void Rules_reject_missing_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => AccessRules.CanManageOrganisation(null!));
        Assert.Throws<ArgumentNullException>(() => AccessRules.CanSeeEmployee(null!, NewEmployee(null)));
        Assert.Throws<ArgumentNullException>(() => AccessRules.CanSeeEmployee(Hr, null!));
    }

    [Fact]
    public async Task Actor_is_read_from_the_user_account()
    {
        var users = new InMemoryUserRepository();
        var company = users.AddTestCompany();
        var resolver = new ActorResolver(users);

        Assert.Equal(
            new Actor(company.Manager.Id, company.Company.Id, UserRole.Manager, company.Manager.EmployeeId),
            await resolver.FindAsync(company.Manager.Id));
        Assert.Null(await resolver.FindAsync(Guid.NewGuid()));
        Assert.Throws<ArgumentNullException>(() => Actor.From(null!));
    }

    private static Employee NewEmployee(Guid? managerId) =>
        new(CompanyId, "Hugo", "Lambert", $"{Guid.NewGuid():N}@example.com", new DateOnly(2022, 5, 2),
            Guid.NewGuid(), Guid.NewGuid(), managerId, TestClock.Now.UtcDateTime);
}

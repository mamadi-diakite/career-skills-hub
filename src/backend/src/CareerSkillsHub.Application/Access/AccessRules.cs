namespace CareerSkillsHub.Application.Access;

/// <summary>Who can see and change what. Every rule also requires the same company.</summary>
public static class AccessRules
{
    /// <summary>Company profile, departments, jobs and the skills framework are managed by HR only.</summary>
    public static bool CanManageOrganisation(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return actor.Role == UserRole.HrAdmin;
    }

    /// <summary>HR sees everyone, a manager sees their team and themselves, an employee sees themselves.</summary>
    public static bool CanSeeEmployee(Actor actor, Employee employee)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(employee);

        if (employee.CompanyId != actor.CompanyId)
        {
            return false;
        }

        if (actor.Role == UserRole.HrAdmin)
        {
            return true;
        }

        return actor.EmployeeId is { } self
            && (employee.Id == self || (actor.Role == UserRole.Manager && employee.ManagerId == self));
    }
}

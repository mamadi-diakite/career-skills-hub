namespace CareerSkillsHub.Infrastructure.Seeding;

public sealed class DemoDataSeeder(CareerSkillsHubDbContext db, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public const string DemoPassword = "CareerDemo123!";

    /// <summary>Seeds the <see cref="DemoCompany"/> with one account per role.</summary>
    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var company = new Company(DemoCompany.Name, DemoCompany.Headcount, now);
        db.Companies.Add(company);

        var departments = DemoCompany.Departments.ToDictionary(name => name, name => new Department(company.Id, name));
        db.Departments.AddRange(departments.Values);

        var skills = new Dictionary<string, Skill>();
        foreach (var demoDomain in DemoCompany.SkillDomains)
        {
            var domain = new SkillDomain(company.Id, demoDomain.Name, demoDomain.Description);
            db.SkillDomains.Add(domain);
            foreach (var s in demoDomain.Skills)
            {
                var skill = new Skill(company.Id, domain.Id, s.Name, s.Description,
                    new SkillLevelDescriptions(s.Beginner, s.Intermediate, s.Advanced, s.Expert));
                skills.Add(s.Name, skill);
                db.Skills.Add(skill);
            }
        }

        var jobs = new Dictionary<string, Job>();
        foreach (var demoJob in DemoCompany.Jobs)
        {
            var job = new Job(company.Id, departments[demoJob.Department].Id, demoJob.Title, demoJob.Description);
            job.SetRequiredSkills(
                [.. demoJob.RequiredSkills.Select(r => new RequiredSkill(skills[r.Skill].Id, r.ExpectedLevel, r.Criticality))]);
            jobs.Add(demoJob.Title, job);
            db.Jobs.Add(job);
        }

        var passwordHash = passwordHasher.Hash(DemoPassword);
        var employees = new Dictionary<string, Employee>();
        foreach (var e in DemoCompany.Employees)
        {
            var (firstJob, firstDepartment) = e.FirstJob is { } first ? (first.Job, first.Department) : (e.Job, e.Department);
            var employee = new Employee(
                company.Id, e.FirstName, e.LastName, e.Email, e.HireDate, jobs[firstJob].Id, departments[firstDepartment].Id,
                e.ManagerEmail is { } managerEmail ? employees[managerEmail].Id : null, now);
            if (e.FirstJob is { } change)
            {
                employee.ChangeJob(jobs[e.Job].Id, departments[e.Department].Id, change.CurrentJobFrom);
            }

            employees.Add(e.Email, employee);
            db.Employees.Add(employee);
            if (e.AccountRole is { } role)
            {
                db.Users.Add(new User(company.Id, e.Email, employee.FullName, passwordHash, role, employee.Id, now));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}

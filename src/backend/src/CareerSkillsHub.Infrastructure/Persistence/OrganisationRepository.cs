namespace CareerSkillsHub.Infrastructure.Persistence;

public sealed class OrganisationRepository(CareerSkillsHubDbContext db) : IOrganisationRepository
{
    public async Task<IReadOnlyList<Department>> ListDepartmentsAsync(Guid companyId, CancellationToken cancellationToken) =>
        await db.Departments.Where(d => d.CompanyId == companyId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Job>> ListJobsAsync(Guid companyId, CancellationToken cancellationToken) =>
        await db.Jobs.Include(j => j.RequiredSkills).Where(j => j.CompanyId == companyId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SkillDomain>> ListSkillDomainsAsync(Guid companyId, CancellationToken cancellationToken) =>
        await db.SkillDomains.Where(d => d.CompanyId == companyId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Skill>> ListSkillsAsync(Guid companyId, CancellationToken cancellationToken) =>
        await db.Skills.Where(s => s.CompanyId == companyId).ToListAsync(cancellationToken);

    public async Task<bool> IsDepartmentUsedAsync(Guid departmentId, CancellationToken cancellationToken) =>
        await db.Jobs.AnyAsync(j => j.DepartmentId == departmentId, cancellationToken)
        || await db.Employees.AnyAsync(e => e.JobHistory.Any(a => a.DepartmentId == departmentId), cancellationToken);

    public Task<bool> IsJobUsedAsync(Guid jobId, CancellationToken cancellationToken) =>
        db.Employees.AnyAsync(e => e.JobHistory.Any(a => a.JobId == jobId), cancellationToken);

    public void Add(Department department) => db.Departments.Add(department);

    public void Add(Job job) => db.Jobs.Add(job);

    public void Add(SkillDomain domain) => db.SkillDomains.Add(domain);

    public void Add(Skill skill) => db.Skills.Add(skill);

    public void Remove(Department department) => db.Departments.Remove(department);

    public void Remove(Job job) => db.Jobs.Remove(job);

    public void Remove(SkillDomain domain) => db.SkillDomains.Remove(domain);

    public void Remove(Skill skill) => db.Skills.Remove(skill);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

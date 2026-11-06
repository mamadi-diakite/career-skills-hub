namespace CareerSkillsHub.Application.Organisation;

/// <remarks>Entities returned by the list methods are tracked: changes to them are saved by <see cref="SaveChangesAsync"/>.</remarks>
public interface IOrganisationRepository
{
    Task<IReadOnlyList<Department>> ListDepartmentsAsync(Guid companyId, CancellationToken cancellationToken);

    /// <summary>Jobs with their required skills.</summary>
    Task<IReadOnlyList<Job>> ListJobsAsync(Guid companyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SkillDomain>> ListSkillDomainsAsync(Guid companyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Skill>> ListSkillsAsync(Guid companyId, CancellationToken cancellationToken);

    /// <returns><c>true</c> when a job, an employee or a past job of an employee belongs to the department.</returns>
    Task<bool> IsDepartmentUsedAsync(Guid departmentId, CancellationToken cancellationToken);

    /// <returns><c>true</c> when an employee holds or held the job.</returns>
    Task<bool> IsJobUsedAsync(Guid jobId, CancellationToken cancellationToken);

    void Add(Department department);

    void Add(Job job);

    void Add(SkillDomain domain);

    void Add(Skill skill);

    void Remove(Department department);

    void Remove(Job job);

    void Remove(SkillDomain domain);

    void Remove(Skill skill);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

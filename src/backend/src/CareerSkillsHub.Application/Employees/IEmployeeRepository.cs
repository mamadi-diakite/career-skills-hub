namespace CareerSkillsHub.Application.Employees;

public interface IEmployeeRepository
{
    /// <summary>Employees of the company, without their job history.</summary>
    Task<IReadOnlyList<Employee>> ListAsync(Guid companyId, CancellationToken cancellationToken);

    /// <summary>An employee of the company with the job history loaded.</summary>
    Task<Employee?> FindAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken);

    void Add(Employee employee);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

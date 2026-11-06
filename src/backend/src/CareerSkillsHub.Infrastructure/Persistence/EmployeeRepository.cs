namespace CareerSkillsHub.Infrastructure.Persistence;

public sealed class EmployeeRepository(CareerSkillsHubDbContext db) : IEmployeeRepository
{
    public async Task<IReadOnlyList<Employee>> ListAsync(Guid companyId, CancellationToken cancellationToken) =>
        await db.Employees.Where(e => e.CompanyId == companyId).ToListAsync(cancellationToken);

    public Task<Employee?> FindAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken) =>
        db.Employees
            .Include(e => e.JobHistory)
            .SingleOrDefaultAsync(e => e.CompanyId == companyId && e.Id == employeeId, cancellationToken);

    public void Add(Employee employee) => db.Employees.Add(employee);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

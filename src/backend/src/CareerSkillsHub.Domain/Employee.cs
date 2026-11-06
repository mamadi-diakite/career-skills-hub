namespace CareerSkillsHub.Domain;

/// <summary>A person working in the company, with the history of the jobs they held.</summary>
public sealed class Employee
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    private readonly List<JobAssignment> _jobHistory = [];

    private Employee()
    {
    }

    public Employee(
        Guid companyId,
        string firstName,
        string lastName,
        string email,
        DateOnly hireDate,
        Guid jobId,
        Guid departmentId,
        Guid? managerId,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        CompanyId = Guard.NotEmpty(companyId, nameof(companyId));
        FirstName = Guard.Required(firstName, nameof(firstName), NameMaxLength);
        LastName = Guard.Required(lastName, nameof(lastName), NameMaxLength);
        Email = Guard.Email(email, nameof(email), EmailMaxLength);
        HireDate = hireDate;
        JobId = Guard.NotEmpty(jobId, nameof(jobId));
        DepartmentId = Guard.NotEmpty(departmentId, nameof(departmentId));
        ManagerId = Guard.NotEmpty(managerId, nameof(managerId));
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));

        _jobHistory.Add(new JobAssignment(Id, JobId, DepartmentId, hireDate));
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";

    public string Email { get; private set; } = string.Empty;

    public DateOnly HireDate { get; private set; }

    /// <summary>Current job, also the last entry of <see cref="JobHistory"/>.</summary>
    public Guid JobId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Guid? ManagerId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Oldest first. Needs to be loaded before <see cref="ChangeJob"/> is called.</summary>
    public IReadOnlyList<JobAssignment> JobHistory => [.. _jobHistory.OrderBy(a => a.StartDate)];

    public static bool IsValidEmail([NotNullWhen(true)] string? email) => Guard.IsEmail(email, EmailMaxLength);

    /// <summary>Closes the current job the day before <paramref name="effectiveDate"/> and starts the new one.</summary>
    public void ChangeJob(Guid jobId, Guid departmentId, DateOnly effectiveDate)
    {
        Guard.NotEmpty(jobId, nameof(jobId));
        Guard.NotEmpty(departmentId, nameof(departmentId));

        var current = _jobHistory.Single(a => a.EndDate is null);
        if (effectiveDate <= current.StartDate)
        {
            throw new ArgumentException("A job change starts after the current job started.", nameof(effectiveDate));
        }

        if (jobId == JobId && departmentId == DepartmentId)
        {
            throw new ArgumentException("The new job or department must differ from the current one.", nameof(jobId));
        }

        current.End(effectiveDate.AddDays(-1));
        _jobHistory.Add(new JobAssignment(Id, jobId, departmentId, effectiveDate));
        JobId = jobId;
        DepartmentId = departmentId;
    }
}

/// <summary>A period during which an employee held a job in a department.</summary>
public sealed class JobAssignment
{
    private JobAssignment()
    {
    }

    internal JobAssignment(Guid employeeId, Guid jobId, Guid departmentId, DateOnly startDate)
    {
        Id = Guid.CreateVersion7();
        EmployeeId = employeeId;
        JobId = jobId;
        DepartmentId = departmentId;
        StartDate = startDate;
    }

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Guid JobId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public DateOnly StartDate { get; private set; }

    /// <summary>Last day in the job, or <c>null</c> for the current job.</summary>
    public DateOnly? EndDate { get; private set; }

    internal void End(DateOnly endDate) => EndDate = endDate;
}

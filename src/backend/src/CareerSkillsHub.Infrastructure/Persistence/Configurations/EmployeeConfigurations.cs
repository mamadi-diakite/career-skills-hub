namespace CareerSkillsHub.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName).HasMaxLength(Employee.NameMaxLength).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(Employee.NameMaxLength).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(Employee.EmailMaxLength).IsRequired();
        builder.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique();
        builder.HasIndex(e => e.ManagerId);

        builder.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Job>().WithMany().HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.JobHistory).WithOne().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.JobHistory).HasField("_jobHistory").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class JobAssignmentConfiguration : IEntityTypeConfiguration<JobAssignment>
{
    public void Configure(EntityTypeBuilder<JobAssignment> builder)
    {
        builder.ToTable("JobAssignments");
        builder.HasKey(a => a.Id);
        // The id is set by the domain, so a new row found through Employee.JobHistory is an insert.
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.HasIndex(a => new { a.EmployeeId, a.StartDate }).IsUnique();

        builder.HasOne<Job>().WithMany().HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(a => a.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

namespace CareerSkillsHub.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(Department.NameMaxLength).IsRequired();
        builder.HasIndex(d => new { d.CompanyId, d.Name }).IsUnique();

        builder.HasOne<Company>().WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title).HasMaxLength(Job.TitleMaxLength).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(Job.DescriptionMaxLength);
        builder.HasIndex(j => new { j.CompanyId, j.Title }).IsUnique();

        builder.HasOne<Company>().WithMany().HasForeignKey(j => j.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(j => j.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(j => j.RequiredSkills).WithOne().HasForeignKey(s => s.JobId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(j => j.RequiredSkills).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class JobRequiredSkillConfiguration : IEntityTypeConfiguration<JobRequiredSkill>
{
    public void Configure(EntityTypeBuilder<JobRequiredSkill> builder)
    {
        builder.ToTable("JobRequiredSkills");
        builder.HasKey(s => new { s.JobId, s.SkillId });

        builder.Property(s => s.ExpectedLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Criticality).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<Skill>().WithMany().HasForeignKey(s => s.SkillId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SkillDomainConfiguration : IEntityTypeConfiguration<SkillDomain>
{
    public void Configure(EntityTypeBuilder<SkillDomain> builder)
    {
        builder.ToTable("SkillDomains");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(SkillDomain.NameMaxLength).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(SkillDomain.DescriptionMaxLength);
        builder.HasIndex(d => new { d.CompanyId, d.Name }).IsUnique();

        builder.HasOne<Company>().WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(Skill.NameMaxLength).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(Skill.DescriptionMaxLength);
        builder.Property(s => s.BeginnerDescription).HasMaxLength(Skill.LevelDescriptionMaxLength).IsRequired();
        builder.Property(s => s.IntermediateDescription).HasMaxLength(Skill.LevelDescriptionMaxLength).IsRequired();
        builder.Property(s => s.AdvancedDescription).HasMaxLength(Skill.LevelDescriptionMaxLength).IsRequired();
        builder.Property(s => s.ExpertDescription).HasMaxLength(Skill.LevelDescriptionMaxLength).IsRequired();
        builder.HasIndex(s => new { s.CompanyId, s.Name }).IsUnique();
        builder.Ignore(s => s.Levels);

        builder.HasOne<Company>().WithMany().HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SkillDomain>().WithMany().HasForeignKey(s => s.DomainId).OnDelete(DeleteBehavior.Restrict);
    }
}

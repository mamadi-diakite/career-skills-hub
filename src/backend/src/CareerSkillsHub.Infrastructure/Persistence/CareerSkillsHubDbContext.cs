namespace CareerSkillsHub.Infrastructure.Persistence;

public sealed class CareerSkillsHubDbContext(DbContextOptions<CareerSkillsHubDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<SkillDomain> SkillDomains => Set<SkillDomain>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CareerSkillsHubDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}

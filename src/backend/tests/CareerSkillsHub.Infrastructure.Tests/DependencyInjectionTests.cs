namespace CareerSkillsHub.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_blank_connection_string(string connectionString)
    {
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddInfrastructure(connectionString));
    }

    [Fact]
    public void Registers_the_repositories_and_the_password_hasher()
    {
        using var provider = new ServiceCollection().AddInfrastructure("Data Source=:memory:").BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<UserRepository>(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.IsType<IdentityPasswordHasher>(provider.GetRequiredService<IPasswordHasher>());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public async Task Creates_missing_data_folder_and_applies_migrations()
    {
        var root = Path.Combine(Path.GetTempPath(), "career-skills-hub-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "data", "app.db");

        try
        {
            await using var provider = new ServiceCollection()
                .AddInfrastructure($"Data Source={databasePath}")
                .BuildServiceProvider();

            await provider.ApplyMigrationsAsync();

            Assert.True(File.Exists(databasePath));
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CareerSkillsHubDbContext>();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Seeds_demo_data_once_through_the_service_provider()
    {
        await using var provider = new ServiceCollection()
            .AddInfrastructure("Data Source=seed-test;Mode=Memory;Cache=Shared")
            .BuildServiceProvider();

        // Keeps the shared in-memory database alive between scopes.
        await using var keepAlive = new SqliteConnection("Data Source=seed-test;Mode=Memory;Cache=Shared");
        await keepAlive.OpenAsync();

        await provider.ApplyMigrationsAsync();

        Assert.True(await provider.SeedDemoDataAsync());
        Assert.False(await provider.SeedDemoDataAsync());
    }

    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=")]
    [InlineData("Data Source=in-memory-test;Mode=Memory;Cache=Shared")]
    public async Task Applies_migrations_to_in_memory_database_without_touching_disk(string connectionString)
    {
        await using var provider = new ServiceCollection()
            .AddInfrastructure(connectionString)
            .BuildServiceProvider();

        await provider.ApplyMigrationsAsync();

        Assert.False(File.Exists("in-memory-test"));
    }
}

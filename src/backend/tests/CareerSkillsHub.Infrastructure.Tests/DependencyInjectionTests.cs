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

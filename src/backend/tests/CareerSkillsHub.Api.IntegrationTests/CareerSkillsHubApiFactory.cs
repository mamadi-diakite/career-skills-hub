namespace CareerSkillsHub.Api.IntegrationTests;

/// <summary>Runs the API against its own throwaway SQLite file.</summary>
public class CareerSkillsHubApiFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "career-skills-hub-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={Path.Combine(_root, "app.db")}");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

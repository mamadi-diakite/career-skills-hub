namespace CareerSkillsHub.Api.IntegrationTests;

/// <summary>Runs the API against its own throwaway SQLite file, with a signing key made for tests.</summary>
public class CareerSkillsHubApiFactory : WebApplicationFactory<Program>
{
    public static readonly string TestSigningKey =
        Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    private readonly string _root = Path.Combine(Path.GetTempPath(), "career-skills-hub-tests", Guid.NewGuid().ToString("N"));

    protected virtual string EnvironmentName => "Development";

    protected virtual bool SeedDemoData => false;

    protected virtual string SigningKey => TestSigningKey;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={Path.Combine(_root, "app.db")}");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", SeedDemoData.ToString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);
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

public sealed class DemoDataApiFactory : CareerSkillsHubApiFactory
{
    protected override bool SeedDemoData => true;
}

public sealed class ProductionDemoDataApiFactory : CareerSkillsHubApiFactory
{
    protected override string EnvironmentName => "Production";

    protected override bool SeedDemoData => true;
}

public sealed class MissingSigningKeyApiFactory : CareerSkillsHubApiFactory
{
    protected override string SigningKey => "";
}

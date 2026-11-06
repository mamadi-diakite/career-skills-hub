namespace CareerSkillsHub.Api.IntegrationTests;

public sealed class DemoDataStartupTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Development_startup_seeds_demo_data_when_enabled()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CareerSkillsHubDbContext>();

        Assert.Equal(DemoCompany.Name, (await db.Companies.SingleAsync()).Name);
        Assert.Equal(3, await db.Users.CountAsync());
    }
}

public sealed class ProductionDemoDataStartupTests(ProductionDemoDataApiFactory factory)
    : IClassFixture<ProductionDemoDataApiFactory>
{
    [Fact]
    public async Task Startup_outside_development_never_seeds_demo_data()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CareerSkillsHubDbContext>();

        Assert.Equal(0, await db.Users.CountAsync());
    }
}

public sealed class MissingSigningKeyStartupTests
{
    [Fact]
    public void Startup_without_a_signing_key_fails()
    {
        using var factory = new MissingSigningKeyApiFactory();

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Jwt:SigningKey is missing", error.Message);
    }
}

public sealed class MissingConnectionStringStartupTests
{
    [Fact]
    public void Startup_without_a_connection_string_fails()
    {
        using var factory = new MissingConnectionStringApiFactory();

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Equal("Connection string 'Default' is missing.", error.Message);
    }
}

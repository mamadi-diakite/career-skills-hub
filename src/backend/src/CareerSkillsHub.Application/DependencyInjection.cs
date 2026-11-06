namespace CareerSkillsHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ActorResolver>();
        services.AddScoped<AuthService>();
        services.AddScoped<CompanyService>();

        return services;
    }
}

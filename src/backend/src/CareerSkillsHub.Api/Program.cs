var builder = WebApplication.CreateBuilder(args);

var jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Default' is missing.");
}

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = jwtSettings.ToValidationParameters();
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyMigrationsAsync();
}

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:SeedDemoData"))
{
    await app.Services.SeedDemoDataAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

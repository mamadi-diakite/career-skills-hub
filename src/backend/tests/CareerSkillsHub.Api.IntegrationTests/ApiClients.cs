namespace CareerSkillsHub.Api.IntegrationTests;

internal static class ApiClients
{
    public const string DemoPassword = DemoDataSeeder.DemoPassword;
    public const string Hr = "claire.fontaine@example.com";
    public const string Manager = "marc.lefebvre@example.com";
    public const string Employee = "ines.moreau@example.com";

    public static async Task<HttpClient> LoggedInAsync(this WebApplicationFactory<Program> factory, string email, string password = DemoPassword)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    /// <summary>Signs up a new company and returns a client logged in as its HR admin.</summary>
    public static async Task<HttpClient> SignedUpAsync(this WebApplicationFactory<Program> factory, string companyName, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/sign-up", new SignUpRequest(companyName, 12, "Jade Roux", email, DemoPassword, DemoPassword));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    /// <summary>Checks the status code and reads the JSON body.</summary>
    public static async Task<T> ReadAsync<T>(this Task<HttpResponseMessage> request, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var response = await request;
        Assert.Equal(expected, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<HttpStatusCode> StatusAsync(this Task<HttpResponseMessage> request) => (await request).StatusCode;
}

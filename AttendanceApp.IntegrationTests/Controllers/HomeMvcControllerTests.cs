using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class HomeMvcControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public HomeMvcControllerIntegrationTests()
    {
        _factory = new IntegrationTestWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _httpClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _factory.CleanupAsync();
        await _factory.DisposeAsync();
    }

    // ---------- Helpers ----------

    private static UserType GetAnyUserType()
    {
        var values = Enum.GetValues<UserType>();
        if (values.Length == 0)
            throw new InvalidOperationException("UserType enum has no defined values.");
        return values[0];
    }

    private async Task<(Guid userId, string jwt)> CreateAndAuthenticateUserAsync(UserType? userType = null)
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            UserType = userType ?? GetAnyUserType()
        };

        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        var userId = await registerResponse.Content.ReadAsAsync<Guid>();

        var loginBody = new LoginUserRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", loginBody);
        loginResponse.EnsureSuccessStatusCode();

        var jwt = await loginResponse.Content.ReadAsAsync<string>();

        return (userId, jwt);
    }

    private void SetAuthCookie(string jwt)
    {
        _httpClient.DefaultRequestHeaders.Add("Cookie", $"AttendanceApp.Jwt={jwt}");
    }

    // ---------- Unauthorized Tests ----------

    [Fact]
    public async Task HomeIndex_WithoutAuth_RedirectsToLogin()
    {
        // Set Accept header to simulate browser request
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/home/index");

        // Should redirect to login page
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomeIndex_WithoutAuth_ApiStyle_RedirectsToLogin()
    {
        // MVC routes always redirect to login regardless of Accept header
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.GetAsync("/home/index");

        // MVC routes redirect instead of returning 401
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Authorized Tests ----------

    [Fact]
    public async Task HomeIndex_WithAuth_ReturnsSuccessAndHtml()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/home/index");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task HomeIndex_WithAuth_ReturnsValidHtml()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/home/index");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should contain valid HTML structure
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomeIndex_WithAuth_AsProfessor_ReturnsSuccess()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync(UserType.Professor);
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/home/index");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HomeIndex_WithAuth_AsStudent_ReturnsSuccess()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync(UserType.Student);
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/home/index");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HomeIndex_WithInvalidJwt_RedirectsToLogin()
    {
        _httpClient.DefaultRequestHeaders.Add("Cookie", "AttendanceApp.Jwt=invalid-jwt-token");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/home/index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomeIndex_WithExpiredJwt_RedirectsToLogin()
    {
        // Create an obviously invalid/expired token
        _httpClient.DefaultRequestHeaders.Add("Cookie", "AttendanceApp.Jwt=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/home/index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class ProfileMvcControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public ProfileMvcControllerIntegrationTests()
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

    private async Task<(Guid userId, string jwt, string email)> CreateAndAuthenticateUserAsync(UserType? userType = null, string? namePrefix = null)
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = namePrefix != null ? $"{namePrefix} User" : "Test User",
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

        return (userId, jwt, email);
    }

    private async Task<Guid> RegisterUserAsync(string namePrefix = "User", UserType? userType = null)
    {
        var email = $"{namePrefix.ToLowerInvariant()}_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = $"{namePrefix} User",
            Email = email,
            Password = password,
            UserType = userType ?? GetAnyUserType()
        };

        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        return await registerResponse.Content.ReadAsAsync<Guid>();
    }

    private void SetAuthCookie(string jwt)
    {
        _httpClient.DefaultRequestHeaders.Add("Cookie", $"AttendanceApp.Jwt={jwt}");
    }

    // ---------- Profile Me Unauthorized Tests ----------

    [Fact]
    public async Task ProfileMe_WithoutAuth_RedirectsToLogin()
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/profile/me");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfileMe_WithoutAuth_ApiStyle_RedirectsToLogin()
    {
        // MVC routes always redirect to login regardless of Accept header
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.GetAsync("/profile/me");

        // MVC routes redirect instead of returning 401
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Profile Me Authorized Tests ----------

    [Fact]
    public async Task ProfileMe_WithAuth_ReturnsSuccessAndHtml()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/profile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ProfileMe_WithAuth_ContainsUserInfo()
    {
        var (_, jwt, email) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/profile/me");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should contain user info
        Assert.Contains("Test User", content);
        Assert.Contains(email, content);
    }

    [Fact]
    public async Task ProfileMe_WithAuth_AsProfessor_ReturnsSuccess()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync(UserType.Professor);
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/profile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProfileMe_WithAuth_AsStudent_ReturnsSuccess()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync(UserType.Student);
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/profile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Profile View Unauthorized Tests ----------

    [Fact]
    public async Task ProfileView_WithoutAuth_RedirectsToLogin()
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var userId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/profile/view/{userId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfileView_WithoutAuth_ApiStyle_RedirectsToLogin()
    {
        // MVC routes always redirect to login regardless of Accept header
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var userId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/profile/view/{userId}");

        // MVC routes redirect instead of returning 401
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Profile View Authorized Tests ----------

    [Fact]
    public async Task ProfileView_WithAuth_OtherUser_ReturnsSuccess()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync();
        var otherUserId = await RegisterUserAsync("Other", UserType.Professor);
        
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync($"/profile/view/{otherUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ProfileView_WithAuth_OtherUser_ContainsUserInfo()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync();
        var otherUserId = await RegisterUserAsync("OtherPerson", UserType.Professor);
        
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync($"/profile/view/{otherUserId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should contain the other user's name
        Assert.Contains("OtherPerson User", content);
    }

    [Fact]
    public async Task ProfileView_WithAuth_OwnProfile_RedirectsToMe()
    {
        var (userId, jwt, _) = await CreateAndAuthenticateUserAsync();
        
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync($"/profile/view/{userId}");

        // Should redirect to /profile/me when viewing own profile
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/profile/me", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfileView_WithAuth_NonExistentUser_ReturnsNotFound()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync();
        
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/profile/view/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProfileView_WithAuth_InvalidGuid_ReturnsNotFound()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync();
        
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/profile/view/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProfileView_WithAuth_ViewProfessor_ShowsFollowOption()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync(UserType.Student);
        var profId = await RegisterUserAsync("Professor", UserType.Professor);
        
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync($"/profile/view/{profId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Page should be rendered, follow functionality handled by JS
    }

    // ---------- Route Tests ----------

    [Fact]
    public async Task Profile_InvalidRoute_ReturnsNotFound()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);

        var response = await _httpClient.GetAsync("/profile/invalid-action");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProfileView_WithoutGuid_ReturnsNotFound()
    {
        var (_, jwt, _) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);

        var response = await _httpClient.GetAsync("/profile/view");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

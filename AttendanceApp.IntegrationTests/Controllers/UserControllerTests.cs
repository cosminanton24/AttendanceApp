using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class UserControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public UserControllerIntegrationTests()
    {
        _factory = new IntegrationTestWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _httpClient = _factory.CreateClient();
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

    /// <summary>
    /// Registers a user and logs them in, then sets the Bearer token
    /// on _httpClient. Returns the created user id and email.
    /// </summary>
    private async Task<(Guid userId, string email)> CreateAndAuthenticateUserAsync()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        // 1) Register
        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            Type = GetAnyUserType()
        };

        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        // Controller returns Guid (Result<Guid>.Created)
        var userId = await registerResponse.Content.ReadAsAsync<Guid>();

        // 2) Login
        var loginBody = new LoginUserRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", loginBody);
        loginResponse.EnsureSuccessStatusCode();

        var jwt = await loginResponse.Content.ReadAsAsync<string>();

        // Use it as Bearer for authorized endpoints
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        return (userId, email);
    }

    /// <summary>
    /// Registers a user but does NOT authenticate as them.
    /// Used for the "professor" that is followed/unfollowed.
    /// </summary>
    private async Task<Guid> RegisterUserAsync(string namePrefix = "Prof", UserType? userType = null)
    {
        var email = $"{namePrefix.ToLowerInvariant()}_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = $"{namePrefix} User",
            Email = email,
            Password = password,
            Type = userType ?? GetAnyUserType()
        };

        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        return await registerResponse.Content.ReadAsAsync<Guid>();
    }

    // ---------- Existing non-authorized test (example) ----------

    [Fact]
    public async Task Register_Then_Login_ReturnsOk_And_SetsJwtCookie()
    {
        // Arrange
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            Type = GetAnyUserType()
        };

        // Act 1: Register
        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);

        // Assert 1
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Act 2: Login
        var loginBody = new LoginUserRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", loginBody);

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        Assert.True(
            loginResponse.Headers.TryGetValues("Set-Cookie", out var setCookieValues),
            "Expected Set-Cookie header to be present on successful login."
        );

        var setCookie = string.Join(";", setCookieValues);
        Assert.Contains("AttendanceApp.Jwt=", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Authorized endpoint tests ----------

    [Fact]
    public async Task GetFollowState_WithoutAuth_ReturnsUnauthorized()
    {
        // Ensure no auth header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var profId = Guid.NewGuid(); // doesn’t matter, auth should fail first
        var response = await _httpClient.GetAsync($"/api/users/following/{profId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ToggleFollowUser_WithoutAuth_ReturnsUnauthorized()
    {
        // Ensure no auth header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var profId = Guid.NewGuid();
        var response = await _httpClient.PostAsync($"/api/users/toggleFollow/{profId}", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowState_WithValidAuth_ReturnsSuccess()
    {
        // Arrange: create "current user" and authenticate
        var (_, _) = await CreateAndAuthenticateUserAsync();

        // Create the "professor" being followed
        var profId = await RegisterUserAsync("Prof", UserType.Professor);

        // Act
        var response = await _httpClient.GetAsync($"/api/users/following/{profId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ToggleFollowUser_WithValidAuth_ReturnsSuccess()
    {
        // Arrange: create "current user" and authenticate
        var (_, _) = await CreateAndAuthenticateUserAsync();

        // Create the "professor" being followed/unfollowed
        var profId = await RegisterUserAsync("Prof", UserType.Professor);

        // Act
        var response = await _httpClient.PostAsync($"/api/users/toggleFollow/{profId}", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

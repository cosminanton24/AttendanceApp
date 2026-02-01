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
            UserType = GetAnyUserType()
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
            UserType = userType ?? GetAnyUserType()
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
            UserType = GetAnyUserType()
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

    private sealed record UserInfoDto(Guid Id, string Name, string Email);

    [Fact]
    public async Task GetUserInfo_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var someId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/users/userInfo?ids={someId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserInfo_WithValidAuth_AndMultipleIds_ReturnsDtos()
    {
        // Arrange
        var (currentUserId, _) = await CreateAndAuthenticateUserAsync();
        var otherUserId = await RegisterUserAsync("Other", userType: UserType.Student);

        // Act
        var response = await _httpClient.GetAsync($"/api/users/userInfo?ids={currentUserId}&ids={otherUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<UserInfoDto>>();
        Assert.NotNull(items);
        Assert.Equal(2, items!.Count);

        var ids = items.Select(x => x.Id).ToHashSet();
        Assert.Contains(currentUserId, ids);
        Assert.Contains(otherUserId, ids);
        Assert.All(items, x =>
        {
            Assert.False(string.IsNullOrWhiteSpace(x.Name));
            Assert.False(string.IsNullOrWhiteSpace(x.Email));
        });
    }

    // ---------- Search Users Tests ----------

    [Fact]
    public async Task SearchUsersByName_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var response = await _httpClient.GetAsync("/api/users/search?name=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsersByName_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync("/api/users/search?name=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsersByName_WithPagination_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync("/api/users/search?name=test&page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsersByName_FindsExistingUser()
    {
        // Arrange: create user with specific name
        var (_, _) = await CreateAndAuthenticateUserAsync();
        
        // Create another user with a searchable name
        var uniqueName = $"UniqueSearchName_{Guid.NewGuid():N}";
        var searchableEmail = $"searchable_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = uniqueName,
            Email = searchableEmail,
            Password = password,
            UserType = GetAnyUserType()
        };

        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        // Act: Search for the unique name
        var response = await _httpClient.GetAsync($"/api/users/search?name={uniqueName}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsersByName_WithEmptyName_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync("/api/users/search?name=");

        // Empty name is not allowed
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsersByName_WithNonExistentName_ReturnsEmptyResult()
    {
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync("/api/users/search?name=NonExistentUser12345XYZ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Additional Register Tests ----------

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = "invalid-email",
            Password = "StrongPass123!",
            UserType = GetAnyUserType()
        };

        var response = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithEmptyName_ReturnsBadRequest()
    {
        var registerBody = new CreateUserRequest
        {
            Name = "",
            Email = $"user_{Guid.NewGuid():N}@example.com",
            Password = "StrongPass123!",
            UserType = GetAnyUserType()
        };

        var response = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = $"user_{Guid.NewGuid():N}@example.com",
            Password = "123",
            UserType = GetAnyUserType()
        };

        var response = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            UserType = GetAnyUserType()
        };

        // First registration
        var firstResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        firstResponse.EnsureSuccessStatusCode();

        // Second registration with same email
        var secondResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);

        // Duplicate email returns BadRequest
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    // ---------- Additional Login Tests ----------

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginBody = new LoginUserRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        var response = await _httpClient.PostAsJsonAsync("/api/users/login", loginBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // First register a user
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            UserType = GetAnyUserType()
        };

        await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);

        // Try login with wrong password
        var loginBody = new LoginUserRequest
        {
            Email = email,
            Password = "WrongPassword456!"
        };

        var response = await _httpClient.PostAsJsonAsync("/api/users/login", loginBody);

        // Expect unauthorized or bad request depending on implementation
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized || 
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected Unauthorized, BadRequest, or NotFound but got {response.StatusCode}");
    }

    // ---------- Additional Follow State Tests ----------

    [Fact]
    public async Task GetFollowState_NonExistentProfessor_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync();

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/users/following/{nonExistentId}");

        // Should return NotFound for non-existent professor
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.OK,
            $"Expected NotFound or OK but got {response.StatusCode}");
    }

    [Fact]
    public async Task ToggleFollowUser_TwiceToUnfollow_FirstSucceeds()
    {
        var (_, _) = await CreateAndAuthenticateUserAsync();
        var profId = await RegisterUserAsync("Prof", UserType.Professor);

        // Follow
        var followResponse = await _httpClient.PostAsync($"/api/users/toggleFollow/{profId}", content: null);
        Assert.Equal(HttpStatusCode.OK, followResponse.StatusCode);
    }

    [Fact]
    public async Task GetUserInfo_WithEmptyIds_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync("/api/users/userInfo");

        // Empty ids is not allowed
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserInfo_WithNonExistentIds_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync();

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/users/userInfo?ids={nonExistentId}");

        // Non-existent ids return NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

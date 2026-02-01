using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class UserFollowingsControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public UserFollowingsControllerIntegrationTests()
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

    private async Task<(Guid userId, string email)> CreateAndAuthenticateUserAsync(UserType? userType = null)
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
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        return (userId, email);
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

    // ---------- GetFollowing Tests ----------

    [Fact]
    public async Task GetFollowing_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var userId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/userFollowings/following/{userId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowing_WithValidAuth_ReturnsSuccess()
    {
        var (userId, _) = await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync($"/api/userFollowings/following/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowing_WithPagination_ReturnsSuccess()
    {
        var (userId, _) = await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync($"/api/userFollowings/following/{userId}?pageIndex=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowing_ForAnotherUser_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync();
        var otherUserId = await RegisterUserAsync("Other");

        var response = await _httpClient.GetAsync($"/api/userFollowings/following/{otherUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowing_AfterFollowingUser_ReturnsFollowedUser()
    {
        var (userId, _) = await CreateAndAuthenticateUserAsync();
        var profId = await RegisterUserAsync("Prof", UserType.Professor);

        // Follow the professor
        var followResponse = await _httpClient.PostAsync($"/api/users/toggleFollow/{profId}", content: null);
        followResponse.EnsureSuccessStatusCode();

        // Get following list
        var response = await _httpClient.GetAsync($"/api/userFollowings/following/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- GetFollowers Tests ----------

    [Fact]
    public async Task GetFollowers_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var userId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/userFollowings/followers/{userId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowers_WithValidAuth_ReturnsSuccess()
    {
        var (userId, _) = await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync($"/api/userFollowings/followers/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowers_WithPagination_ReturnsSuccess()
    {
        var (userId, _) = await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync($"/api/userFollowings/followers/{userId}?pageIndex=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowers_ForAnotherUser_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync();
        var otherUserId = await RegisterUserAsync("Other");

        var response = await _httpClient.GetAsync($"/api/userFollowings/followers/{otherUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowers_AfterBeingFollowed_ReturnsFollower()
    {
        // Create and auth as a student
        var (studentId, studentEmail) = await CreateAndAuthenticateUserAsync(UserType.Student);

        // Create a professor
        var profEmail = $"prof_{Guid.NewGuid():N}@example.com";
        var profPassword = "StrongPass123!";

        _httpClient.DefaultRequestHeaders.Authorization = null;

        var profRegisterBody = new CreateUserRequest
        {
            Name = "Professor User",
            Email = profEmail,
            Password = profPassword,
            UserType = UserType.Professor
        };

        var profRegisterResponse = await _httpClient.PostAsJsonAsync("/api/users/register", profRegisterBody);
        profRegisterResponse.EnsureSuccessStatusCode();
        var profId = await profRegisterResponse.Content.ReadAsAsync<Guid>();

        // Login back as student and follow the professor
        var studentLoginBody = new LoginUserRequest
        {
            Email = studentEmail,
            Password = "StrongPass123!"
        };
        var studentLoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", studentLoginBody);
        var studentJwt = await studentLoginResponse.Content.ReadAsAsync<string>();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentJwt);

        var followResponse = await _httpClient.PostAsync($"/api/users/toggleFollow/{profId}", content: null);
        followResponse.EnsureSuccessStatusCode();

        // Get followers of professor
        var response = await _httpClient.GetAsync($"/api/userFollowings/followers/{profId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowing_NonExistentUser_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync();

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/userFollowings/following/{nonExistentId}");

        // Non-existent user returns NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFollowers_NonExistentUser_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync();

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/userFollowings/followers/{nonExistentId}");

        // Non-existent user returns NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

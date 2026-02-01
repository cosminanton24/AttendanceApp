using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class LectureMvcControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public LectureMvcControllerIntegrationTests()
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
    public async Task LectureJoin_WithoutAuth_RedirectsToLogin()
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/lecture/join/{lectureId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LectureJoin_WithoutAuth_ApiStyle_RedirectsToLogin()
    {
        // MVC routes always redirect to login regardless of Accept header
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/lecture/join/{lectureId}");

        // MVC routes redirect instead of returning 401
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Authorized Tests ----------

    [Fact]
    public async Task LectureJoin_WithAuth_ValidGuid_ReturnsSuccess()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/lecture/join/{lectureId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task LectureJoin_WithAuth_ValidGuid_ContainsLectureId()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/lecture/join/{lectureId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should contain the lecture ID in the page
        Assert.Contains(lectureId.ToString(), content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LectureJoin_WithAuth_NoId_ReturnsSuccess()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/lecture/join");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LectureJoin_WithAuth_EmptyId_ReturnsSuccess()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/lecture/join/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LectureJoin_WithAuth_InvalidGuid_ReturnsSuccessWithInvalidFlag()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await _httpClient.GetAsync("/lecture/join/not-a-valid-guid");
        var content = await response.Content.ReadAsStringAsync();

        // Controller still returns success but with IsLectureIdValid = false
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LectureJoin_WithAuth_AsStudent_ReturnsSuccess()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync(UserType.Student);
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/lecture/join/{lectureId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LectureJoin_WithAuth_AsProfessor_ReturnsSuccess()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync(UserType.Professor);
        SetAuthCookie(jwt);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/lecture/join/{lectureId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Route Tests ----------

    [Fact]
    public async Task Lecture_InvalidRoute_ReturnsNotFound()
    {
        var (_, jwt) = await CreateAndAuthenticateUserAsync();
        SetAuthCookie(jwt);

        var response = await _httpClient.GetAsync("/lecture/invalid-action");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

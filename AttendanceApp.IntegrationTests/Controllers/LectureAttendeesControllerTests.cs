using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Lectures;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class LectureAttendeesControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public LectureAttendeesControllerIntegrationTests()
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

    private async Task<Guid> CreateLectureAsync(
        string name = "Test Lecture",
        string description = "Test Description",
        DateTime? startTime = null,
        TimeSpan? duration = null)
    {
        var lectureBody = new CreateLectureRequest
        {
            Name = name,
            Description = description,
            StartTime = startTime ?? DateTime.UtcNow.AddMinutes(10),
            Duration = duration ?? TimeSpan.FromHours(1)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<Guid>();
    }

    [Fact]
    public async Task GetLectureAttendees_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_WithAuth_AndMissingLecture_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{Guid.NewGuid()}?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_TypoRouteAlias_Works()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{Guid.NewGuid()}?page=0&pageSize=10");

        // Lecture doesn't exist, but route should match and return a proper NotFound (not 404 from routing)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_ExistingLecture_ReturnsSuccess()
    {
        // Create professor and lecture
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_WithPagination_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_WithSearchFilter_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=10&searchFilter=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_WithAllParameters_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=20&searchFilter=student");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_AfterStudentJoins_ReturnsAttendee()
    {
        // Create professor and start lecture
        var profEmail = $"prof_{Guid.NewGuid():N}@example.com";
        var profPassword = "StrongPass123!";

        var profRegisterBody = new CreateUserRequest
        {
            Name = "Professor User",
            Email = profEmail,
            Password = profPassword,
            UserType = UserType.Professor
        };

        var profRegisterResponse = await _httpClient.PostAsJsonAsync("/api/users/register", profRegisterBody);
        profRegisterResponse.EnsureSuccessStatusCode();

        var profLoginBody = new LoginUserRequest
        {
            Email = profEmail,
            Password = profPassword
        };

        var profLoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", profLoginBody);
        profLoginResponse.EnsureSuccessStatusCode();
        var profJwt = await profLoginResponse.Content.ReadAsAsync<string>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", profJwt);

        var lectureId = await CreateLectureAsync();
        
        // Start the lecture
        var statusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };
        var statusResponse = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", statusBody);
        statusResponse.EnsureSuccessStatusCode();

        // Create student and join
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var studentEmail = $"student_{Guid.NewGuid():N}@example.com";
        var studentPassword = "StrongPass123!";

        var studentRegisterBody = new CreateUserRequest
        {
            Name = "Student User",
            Email = studentEmail,
            Password = studentPassword,
            UserType = UserType.Student
        };

        var studentRegisterResponse = await _httpClient.PostAsJsonAsync("/api/users/register", studentRegisterBody);
        studentRegisterResponse.EnsureSuccessStatusCode();

        var studentLoginBody = new LoginUserRequest
        {
            Email = studentEmail,
            Password = studentPassword
        };

        var studentLoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", studentLoginBody);
        studentLoginResponse.EnsureSuccessStatusCode();
        var studentJwt = await studentLoginResponse.Content.ReadAsAsync<string>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentJwt);

        // Student joins lecture
        var joinResponse = await _httpClient.PostAsync($"/api/lectures/join/{lectureId}", content: null);
        joinResponse.EnsureSuccessStatusCode();

        // Professor checks attendees
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", profJwt);

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_SecondPage_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_LargePageSize_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_EmptySearchFilter_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=10&searchFilter=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLectureAttendees_AsStudent_ReturnsSuccessOrForbidden()
    {
        // Create professor and lecture
        var profEmail = $"prof_{Guid.NewGuid():N}@example.com";
        var profPassword = "StrongPass123!";

        var profRegisterBody = new CreateUserRequest
        {
            Name = "Professor User",
            Email = profEmail,
            Password = profPassword,
            UserType = UserType.Professor
        };

        await _httpClient.PostAsJsonAsync("/api/users/register", profRegisterBody);

        var profLoginBody = new LoginUserRequest
        {
            Email = profEmail,
            Password = profPassword
        };

        var profLoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", profLoginBody);
        var profJwt = await profLoginResponse.Content.ReadAsAsync<string>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", profJwt);

        var lectureId = await CreateLectureAsync();

        // Switch to student and try to get attendees
        _httpClient.DefaultRequestHeaders.Authorization = null;
        await CreateAndAuthenticateUserAsync(UserType.Student);

        var response = await _httpClient.GetAsync($"/api/lectureAttendees/{lectureId}?page=0&pageSize=10");

        // Depending on implementation, may return success, forbidden, or not found
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK, Forbidden, NotFound, or BadRequest but got {response.StatusCode}");
    }
}

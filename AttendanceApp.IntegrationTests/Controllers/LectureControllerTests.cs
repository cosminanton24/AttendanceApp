using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Lectures;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class LectureControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public LectureControllerIntegrationTests()
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
    private async Task<(Guid userId, string email)> CreateAndAuthenticateUserAsync(UserType? type = null)
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        // 1) Register
        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            Type = type ?? GetAnyUserType()
        };

        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

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
    /// </summary>
    private async Task<Guid> RegisterUserAsync(string namePrefix = "User", UserType? userType = null)
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

    /// <summary>
    /// Creates a lecture for the currently authenticated user.
    /// </summary>
    private async Task<Guid> CreateLectureAsync(
        string name = "Test Lecture",
        string description = "Test Description",
        DateTime? startTime = null,
        TimeSpan? duration = null)
    {
        var lectureName = $"{name}_{Guid.NewGuid():N}";
        var lectureBody = new CreateLectureRequest
        {
            Name = lectureName,
            Description = description,
            StartTime = startTime ?? DateTime.UtcNow.AddHours(1),
            Duration = duration ?? TimeSpan.FromHours(1)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<Guid>();
    }

    // ---------- Unauthorized endpoint tests ----------

    [Fact]
    public async Task CreateLecture_WithoutAuth_ReturnsUnauthorized()
    {
        // Ensure no auth header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var lectureBody = new CreateLectureRequest
        {
            Name = "Test Lecture",
            Description = "Test Description",
            StartTime = DateTime.UtcNow.AddHours(1),
            Duration = TimeSpan.FromHours(1)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfessorLectures_WithoutAuth_ReturnsUnauthorized()
    {
        // Ensure no auth header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var profId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/lectures/{profId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task JoinLecture_WithoutAuth_ReturnsUnauthorized()
    {
        // Ensure no auth header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.PostAsync($"/api/lectures/join/{lectureId}", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLectures_WithoutAuth_ReturnsUnauthorized()
    {
        // Ensure no auth header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var response = await _httpClient.GetAsync("/api/lectures/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLectureStatus_WithoutAuth_ReturnsUnauthorized()
    {
        // Ensure no auth header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var lectureId = Guid.NewGuid();
        var statusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };

        var response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", statusBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Authorized endpoint tests ----------

    [Fact]
    public async Task CreateLecture_WithValidAuth_ReturnsCreated()
    {
        // Arrange: create and authenticate user
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var lectureBody = new CreateLectureRequest
        {
            Name = $"Test Lecture {Guid.NewGuid():N}",
            Description = "Test Description",
            StartTime = DateTime.UtcNow.AddHours(1),
            Duration = TimeSpan.FromHours(1)
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lectureId = await response.Content.ReadAsAsync<Guid>();
        Assert.NotEqual(Guid.Empty, lectureId);
    }

    [Fact]
    public async Task GetProfessorLectures_WithValidAuth_ReturnsSuccess()
    {
        // Arrange: create professor and authenticate as student
        var profId = await RegisterUserAsync("Prof", UserType.Professor);
        await CreateAndAuthenticateUserAsync();

        // Act
        var response = await _httpClient.GetAsync($"/api/lectures/{profId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfessorLectures_WithPagination_ReturnsSuccess()
    {
        // Arrange: create professor and authenticate as student
        var profId = await RegisterUserAsync("Prof", UserType.Professor);
        await CreateAndAuthenticateUserAsync();

        // Act
        var response = await _httpClient.GetAsync($"/api/lectures/{profId}?page=0&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLectures_WithValidAuth_ReturnsSuccess()
    {
        // Arrange: create and authenticate user (student)
        await CreateAndAuthenticateUserAsync();

        // Act
        var response = await _httpClient.GetAsync("/api/lectures/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLectures_WithStatusFilter_ReturnsSuccess()
    {
        // Arrange: create and authenticate user (student)
        await CreateAndAuthenticateUserAsync();

        // Act
        var response = await _httpClient.GetAsync("/api/lectures/?status=Scheduled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JoinLecture_WithValidAuth_ReturnsSuccess()
    {
        // Arrange: create professor, create lecture, and authenticate as student
        var (profId, _) = await CreateAndAuthenticateUserAsync();

        // Switch to professor context to create lecture
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var profEmail = $"prof_{Guid.NewGuid():N}@example.com";
        var profPassword = "StrongPass123!";
        
        var profRegisterBody = new CreateUserRequest
        {
            Name = "Professor User",
            Email = profEmail,
            Password = profPassword,
            Type = UserType.Professor
        };
        
        var profRegisterResponse = await _httpClient.PostAsJsonAsync("/api/users/register", profRegisterBody);
        profRegisterResponse.EnsureSuccessStatusCode();
        var actualProfId = await profRegisterResponse.Content.ReadAsAsync<Guid>();

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
        var changeLectureStatusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };
        var changeStatusResponse = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", changeLectureStatusBody);
        changeStatusResponse.EnsureSuccessStatusCode();

        // Switch back to student context
        var studentEmail = $"student_{Guid.NewGuid():N}@example.com";
        var studentPassword = "StrongPass123!";

        _httpClient.DefaultRequestHeaders.Authorization = null;

        var studentRegisterBody = new CreateUserRequest
        {
            Name = "Student User",
            Email = studentEmail,
            Password = studentPassword,
            Type = GetAnyUserType()
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

        // Act
        var response = await _httpClient.PostAsync($"/api/lectures/join/{lectureId}", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLectureStatus_WithValidAuth_ReturnsSuccess()
    {
        // Arrange: create and authenticate user (professor), create lecture
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var statusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", statusBody);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLectureStatus_ToEnded_ReturnsSuccess()
    {
        // Arrange: create and authenticate user, create lecture
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var statusBody = new UpdateLectureStatusRequest { Status = LectureStatus.Ended };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", statusBody);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLectureStatus_ToCanceled_ReturnsSuccess()
    {
        // Arrange: create and authenticate user, create lecture
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var statusBody = new UpdateLectureStatusRequest { Status = LectureStatus.Canceled };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", statusBody);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

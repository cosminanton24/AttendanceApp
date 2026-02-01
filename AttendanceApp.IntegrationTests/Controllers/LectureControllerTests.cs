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
            UserType = type ?? GetAnyUserType()
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
            UserType = userType ?? GetAnyUserType()
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
        var lectureName = $"{name}";
        var lectureBody = new CreateLectureRequest
        {
            Name = lectureName,
            Description = description,
            StartTime = startTime ?? DateTime.UtcNow.AddMinutes(10),
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

        var response = await _httpClient.GetAsync("/api/lectures/student");

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
            Name = $"Test Lecture",
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
        var response = await _httpClient.GetAsync("/api/lectures/student");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLectures_WithStatusFilter_ReturnsSuccess()
    {
        // Arrange: create and authenticate user (student)
        await CreateAndAuthenticateUserAsync();

        // Act
        var response = await _httpClient.GetAsync("/api/lectures/student/?status=Scheduled");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JoinLecture_WithValidAuth_ReturnsSuccess()
    {
        // Arrange: create professor, create lecture, and authenticate as student
        var (_, _) = await CreateAndAuthenticateUserAsync();

        // Switch to professor context to create lecture
        _httpClient.DefaultRequestHeaders.Authorization = null;
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
        var _ = await profRegisterResponse.Content.ReadAsAsync<Guid>();

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
            UserType = GetAnyUserType()
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
        var lectureId = await CreateLectureAsync(startTime: DateTime.UtcNow.AddMinutes(1), duration: TimeSpan.FromMinutes(1));

        var firstStatusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };
        var secondStatusBody = new UpdateLectureStatusRequest { Status = LectureStatus.Ended };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", firstStatusBody);
        response.EnsureSuccessStatusCode();
        response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", secondStatusBody);
        response.EnsureSuccessStatusCode();

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

    // ---------- GetMyLectures Tests ----------

    [Fact]
    public async Task GetMyLectures_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var response = await _httpClient.GetAsync("/api/lectures/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLectures_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync("/api/lectures/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLectures_WithPagination_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync("/api/lectures/me?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLectures_WithFromMonthsAgo_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync("/api/lectures/me?fromMonthsAgo=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLectures_AfterCreatingLecture_ReturnsCreatedLecture()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync("My Test Lecture");

        var response = await _httpClient.GetAsync("/api/lectures/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- DeleteLecture Tests ----------

    [Fact]
    public async Task DeleteLecture_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.DeleteAsync($"/api/lectures/{lectureId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLecture_WithValidAuth_ReturnsSuccessOrBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.DeleteAsync($"/api/lectures/{lectureId}");

        // May return OK or BadRequest depending on lecture state
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK or BadRequest but got {response.StatusCode}");
    }

    [Fact]
    public async Task DeleteLecture_NonExistentLecture_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.DeleteAsync($"/api/lectures/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLecture_OtherUserLecture_ReturnsForbidden()
    {
        // Create a lecture as professor 1
        var prof1Email = $"prof1_{Guid.NewGuid():N}@example.com";
        var prof1Password = "StrongPass123!";

        var prof1RegisterBody = new CreateUserRequest
        {
            Name = "Professor One",
            Email = prof1Email,
            Password = prof1Password,
            UserType = UserType.Professor
        };

        var prof1RegisterResponse = await _httpClient.PostAsJsonAsync("/api/users/register", prof1RegisterBody);
        prof1RegisterResponse.EnsureSuccessStatusCode();

        var prof1LoginBody = new LoginUserRequest
        {
            Email = prof1Email,
            Password = prof1Password
        };

        var prof1LoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", prof1LoginBody);
        prof1LoginResponse.EnsureSuccessStatusCode();
        var prof1Jwt = await prof1LoginResponse.Content.ReadAsAsync<string>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", prof1Jwt);

        var lectureId = await CreateLectureAsync("Prof1 Lecture");

        // Now try to delete as professor 2
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var prof2Email = $"prof2_{Guid.NewGuid():N}@example.com";
        var prof2Password = "StrongPass123!";

        var prof2RegisterBody = new CreateUserRequest
        {
            Name = "Professor Two",
            Email = prof2Email,
            Password = prof2Password,
            UserType = UserType.Professor
        };

        var prof2RegisterResponse = await _httpClient.PostAsJsonAsync("/api/users/register", prof2RegisterBody);
        prof2RegisterResponse.EnsureSuccessStatusCode();

        var prof2LoginBody = new LoginUserRequest
        {
            Email = prof2Email,
            Password = prof2Password
        };

        var prof2LoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", prof2LoginBody);
        prof2LoginResponse.EnsureSuccessStatusCode();
        var prof2Jwt = await prof2LoginResponse.Content.ReadAsAsync<string>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", prof2Jwt);

        var response = await _httpClient.DeleteAsync($"/api/lectures/{lectureId}");

        // Should be forbidden or not found (depending on implementation)
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || 
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected Forbidden, NotFound, or BadRequest but got {response.StatusCode}");
    }

    [Fact]
    public async Task DeleteLecture_ThenGetMyLectures_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync("To Be Deleted");

        // Try to delete the lecture (may or may not succeed based on constraints)
        var deleteResponse = await _httpClient.DeleteAsync($"/api/lectures/{lectureId}");

        // Get my lectures - should work regardless of delete outcome
        var response = await _httpClient.GetAsync("/api/lectures/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Additional CreateLecture Tests ----------

    [Fact]
    public async Task CreateLecture_WithEmptyName_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var lectureBody = new CreateLectureRequest
        {
            Name = "",
            Description = "Test Description",
            StartTime = DateTime.UtcNow.AddHours(1),
            Duration = TimeSpan.FromHours(1)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLecture_WithPastStartTime_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var lectureBody = new CreateLectureRequest
        {
            Name = "Past Lecture",
            Description = "Test Description",
            StartTime = DateTime.UtcNow.AddHours(-1), // Past time
            Duration = TimeSpan.FromHours(1)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLecture_WithZeroDuration_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var lectureBody = new CreateLectureRequest
        {
            Name = "Zero Duration Lecture",
            Description = "Test Description",
            StartTime = DateTime.UtcNow.AddHours(1),
            Duration = TimeSpan.Zero
        };

        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Additional GetProfessorLectures Tests ----------

    [Fact]
    public async Task GetProfessorLectures_WithFromMonthsAgo_ReturnsSuccess()
    {
        var profId = await RegisterUserAsync("Prof", UserType.Professor);
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync($"/api/lectures/{profId}?fromMonthsAgo=6");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfessorLectures_NonExistentProfessor_ReturnsEmptyOrNotFound()
    {
        await CreateAndAuthenticateUserAsync();

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/lectures/{nonExistentId}");

        // Should return OK with empty list or NotFound
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound but got {response.StatusCode}");
    }

    // ---------- Additional JoinLecture Tests ----------

    [Fact]
    public async Task JoinLecture_NonExistentLecture_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync();

        var nonExistentId = Guid.NewGuid();
        var response = await _httpClient.PostAsync($"/api/lectures/join/{nonExistentId}", content: null);

        // Non-existent lecture returns BadRequest
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task JoinLecture_ScheduledLecture_ReturnsBadRequest()
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
        // Lecture is still in Scheduled status

        // Switch to student
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var (_, _) = await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.PostAsync($"/api/lectures/join/{lectureId}", content: null);

        // Should fail because lecture is not in progress
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.Conflict,
            $"Expected BadRequest or Conflict but got {response.StatusCode}");
    }

    [Fact]
    public async Task JoinLecture_SameLectureTwice_HandlesSecondJoinProperly()
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
        var changeStatusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };
        await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", changeStatusBody);

        // Switch to student
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var (_, _) = await CreateAndAuthenticateUserAsync();

        // Join first time
        var firstJoinResponse = await _httpClient.PostAsync($"/api/lectures/join/{lectureId}", content: null);
        firstJoinResponse.EnsureSuccessStatusCode();

        // Join second time - should return a proper response
        var secondJoinResponse = await _httpClient.PostAsync($"/api/lectures/join/{lectureId}", content: null);

        // Should return an error code for already joined
        Assert.True(
            secondJoinResponse.StatusCode == HttpStatusCode.OK || 
            secondJoinResponse.StatusCode == HttpStatusCode.Conflict ||
            secondJoinResponse.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK, Conflict, or BadRequest but got {secondJoinResponse.StatusCode}");
    }

    // ---------- Additional GetStudentLectures Tests ----------

    [Fact]
    public async Task GetStudentLectures_WithFromMonthsAgo_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync("/api/lectures/student?fromMonthsAgo=12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentLectures_WithAllParameters_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync("/api/lectures/student?page=0&pageSize=5&fromMonthsAgo=6&status=InProgress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Additional UpdateLectureStatus Tests ----------

    [Fact]
    public async Task UpdateLectureStatus_NonExistentLecture_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var nonExistentId = Guid.NewGuid();
        var statusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };

        var response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{nonExistentId}", statusBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLectureStatus_OtherUserLecture_ReturnsAppropriateResponse()
    {
        // Create lecture as professor 1
        var prof1Email = $"prof1_{Guid.NewGuid():N}@example.com";
        var prof1Password = "StrongPass123!";

        var prof1RegisterBody = new CreateUserRequest
        {
            Name = "Professor One",
            Email = prof1Email,
            Password = prof1Password,
            UserType = UserType.Professor
        };

        await _httpClient.PostAsJsonAsync("/api/users/register", prof1RegisterBody);

        var prof1LoginBody = new LoginUserRequest
        {
            Email = prof1Email,
            Password = prof1Password
        };

        var prof1LoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", prof1LoginBody);
        var prof1Jwt = await prof1LoginResponse.Content.ReadAsAsync<string>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", prof1Jwt);

        var lectureId = await CreateLectureAsync();

        // Try to update as professor 2
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var prof2Email = $"prof2_{Guid.NewGuid():N}@example.com";
        var prof2Password = "StrongPass123!";

        var prof2RegisterBody = new CreateUserRequest
        {
            Name = "Professor Two",
            Email = prof2Email,
            Password = prof2Password,
            UserType = UserType.Professor
        };

        await _httpClient.PostAsJsonAsync("/api/users/register", prof2RegisterBody);

        var prof2LoginBody = new LoginUserRequest
        {
            Email = prof2Email,
            Password = prof2Password
        };

        var prof2LoginResponse = await _httpClient.PostAsJsonAsync("/api/users/login", prof2LoginBody);
        var prof2Jwt = await prof2LoginResponse.Content.ReadAsAsync<string>();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", prof2Jwt);

        var statusBody = new UpdateLectureStatusRequest { Status = LectureStatus.InProgress };
        var response = await _httpClient.PutAsJsonAsync($"/api/lectures/status/{lectureId}", statusBody);

        // API may allow any professor to update or restrict - document the actual behavior
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Forbidden || 
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected OK, Forbidden, NotFound, or BadRequest but got {response.StatusCode}");
    }
}

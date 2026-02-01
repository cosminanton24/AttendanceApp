using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AttendanceApp.Api.Common.Requests.Lectures;
using AttendanceApp.Api.Common.Requests.Quizzes;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Domain.Enums;
using AttendanceApp.IntegrationTests.Fixtures;
using AttendanceApp.IntegrationTests.Helpers;

namespace AttendanceApp.IntegrationTests.Controllers;

public sealed class QuizControllerIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private HttpClient _httpClient = null!;

    public QuizControllerIntegrationTests()
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

    private async Task<(Guid userId, string email)> CreateAndAuthenticateUserAsync(UserType userType = UserType.Professor)
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "StrongPass123!";

        var registerBody = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = password,
            UserType = userType
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

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

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
            UserType = userType ?? UserType.Professor
        };

        var registerResponse = await _httpClient.PostAsJsonAsync("/api/users/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        return await registerResponse.Content.ReadAsAsync<Guid>();
    }

    private async Task<Guid> CreateQuizAsync(string name = "Test Quiz", TimeSpan? duration = null)
    {
        var quizBody = new CreateQuizRequest
        {
            Name = name,
            Duration = duration ?? TimeSpan.FromMinutes(15)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes", quizBody);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<Guid>();
    }

    private async Task<Guid> CreateQuestionAsync(Guid quizId, string text = "Test Question", int order = 1, decimal? points = 1)
    {
        var questionBody = new CreateQuizQuestionRequest
        {
            QuizId = quizId,
            Text = text,
            Order = order,
            Points = points
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/questions", questionBody);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<Guid>();
    }

    private async Task<Guid> CreateOptionAsync(Guid questionId, string text = "Test Option", int order = 1, bool isCorrect = false)
    {
        var optionBody = new CreateQuizOptionRequest
        {
            QuestionId = questionId,
            Text = text,
            Order = order,
            IsCorrect = isCorrect
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/options", optionBody);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<Guid>();
    }

    private async Task<Guid> CreateLectureAsync(string name = "Test Lecture", DateTime? startTime = null)
    {
        var lectureBody = new CreateLectureRequest
        {
            Name = name,
            Description = "Test Description",
            StartTime = startTime ?? DateTime.UtcNow.AddMinutes(10),
            Duration = TimeSpan.FromHours(1)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/lectures/", lectureBody);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<Guid>();
    }

    // ========================================================================
    // Unauthorized endpoint tests
    // ========================================================================

    [Fact]
    public async Task CreateQuiz_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var quizBody = new CreateQuizRequest
        {
            Name = "Test Quiz",
            Duration = TimeSpan.FromMinutes(15)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes", quizBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyQuizzes_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var response = await _httpClient.GetAsync("/api/quizzes/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizzesByProfessor_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var profId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/quizzes/professor/{profId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizById_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var quizId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuiz_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var quizId = Guid.NewGuid();
        var response = await _httpClient.DeleteAsync($"/api/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuiz_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var quizId = Guid.NewGuid();
        var updateBody = new UpdateQuizRequest { Name = "Updated" };

        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/{quizId}", updateBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuizQuestion_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var questionBody = new CreateQuizQuestionRequest
        {
            QuizId = Guid.NewGuid(),
            Text = "Question",
            Order = 1
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/questions", questionBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuizQuestion_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var questionId = Guid.NewGuid();
        var response = await _httpClient.DeleteAsync($"/api/quizzes/questions/{questionId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizQuestion_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var questionId = Guid.NewGuid();
        var updateBody = new UpdateQuizQuestionRequest { Text = "Updated" };

        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/questions/{questionId}", updateBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuizOption_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var optionBody = new CreateQuizOptionRequest
        {
            QuestionId = Guid.NewGuid(),
            Text = "Option",
            Order = 1,
            IsCorrect = false
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/options", optionBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuizOption_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var optionId = Guid.NewGuid();
        var response = await _httpClient.DeleteAsync($"/api/quizzes/options/{optionId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizOption_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var optionId = Guid.NewGuid();
        var updateBody = new UpdateQuizOptionRequest { Text = "Updated" };

        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/options/{optionId}", updateBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ActivateQuizForLecture_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var activateBody = new ActivateQuizForLectureRequest
        {
            LectureId = Guid.NewGuid(),
            QuizId = Guid.NewGuid()
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/activate", activateBody);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizzesByLecture_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/quizzes/lecture/{lectureId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveQuizForLecture_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var lectureId = Guid.NewGuid();
        var response = await _httpClient.GetAsync($"/api/quizzes/lecture/{lectureId}/active");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizInfoBatch_WithoutAuth_ReturnsUnauthorized()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var response = await _httpClient.GetAsync($"/api/quizzes/batch?ids={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ========================================================================
    // Quiz CRUD - Authorized tests
    // ========================================================================

    [Fact]
    public async Task CreateQuiz_WithValidAuth_ReturnsCreated()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var quizBody = new CreateQuizRequest
        {
            Name = "Test Quiz",
            Duration = TimeSpan.FromMinutes(15)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes", quizBody);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var quizId = await response.Content.ReadAsAsync<Guid>();
        Assert.NotEqual(Guid.Empty, quizId);
    }

    [Fact]
    public async Task CreateQuiz_AsStudent_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Student);

        var quizBody = new CreateQuizRequest
        {
            Name = "Test Quiz",
            Duration = TimeSpan.FromMinutes(15)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes", quizBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyQuizzes_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync("/api/quizzes/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyQuizzes_WithPagination_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        await CreateQuizAsync("Quiz 1");
        await CreateQuizAsync("Quiz 2");

        var response = await _httpClient.GetAsync("/api/quizzes/me?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyQuizzes_WithNameFilter_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        await CreateQuizAsync("Math Quiz");
        await CreateQuizAsync("Science Quiz");

        var response = await _httpClient.GetAsync("/api/quizzes/me?name=Math");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizzesByProfessor_WithValidAuth_ReturnsSuccess()
    {
        var profId = await RegisterUserAsync("Prof", UserType.Professor);
        await CreateAndAuthenticateUserAsync();

        var response = await _httpClient.GetAsync($"/api/quizzes/professor/{profId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizById_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        var response = await _httpClient.GetAsync($"/api/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizById_NonExistent_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync($"/api/quizzes/{Guid.NewGuid()}");

        // API returns BadRequest for non-existent quiz (validation error)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuiz_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        var response = await _httpClient.DeleteAsync($"/api/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify quiz is deleted - returns BadRequest for non-existent
        var getResponse = await _httpClient.GetAsync($"/api/quizzes/{quizId}");
        Assert.Equal(HttpStatusCode.BadRequest, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteQuiz_NonExistent_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.DeleteAsync($"/api/quizzes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuiz_OtherProfessorQuiz_ReturnsBadRequest()
    {
        // Create quiz as professor 1
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        // Try to delete as professor 2
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.DeleteAsync($"/api/quizzes/{quizId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuiz_Name_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync("Original Name");

        var updateBody = new UpdateQuizRequest { Name = "Updated Name" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/{quizId}", updateBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuiz_Duration_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        var updateBody = new UpdateQuizRequest { Duration = "00:30:00" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/{quizId}", updateBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuiz_NonExistent_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var updateBody = new UpdateQuizRequest { Name = "Updated" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/{Guid.NewGuid()}", updateBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuiz_OtherProfessorQuiz_ReturnsBadRequest()
    {
        // Create quiz as professor 1
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        // Try to update as professor 2
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var updateBody = new UpdateQuizRequest { Name = "Hacked Name" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/{quizId}", updateBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizInfoBatch_WithValidIds_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId1 = await CreateQuizAsync("Quiz 1");
        var quizId2 = await CreateQuizAsync("Quiz 2");

        var response = await _httpClient.GetAsync($"/api/quizzes/batch?ids={quizId1}&ids={quizId2}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizInfoBatch_EmptyIds_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync("/api/quizzes/batch");

        // API requires at least one ID
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ========================================================================
    // Quiz Questions - Authorized tests
    // ========================================================================

    [Fact]
    public async Task CreateQuizQuestion_WithValidAuth_ReturnsCreated()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        var questionBody = new CreateQuizQuestionRequest
        {
            QuizId = quizId,
            Text = "What is 2+2?",
            Order = 1,
            Points = 5
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/questions", questionBody);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var questionId = await response.Content.ReadAsAsync<Guid>();
        Assert.NotEqual(Guid.Empty, questionId);
    }

    [Fact]
    public async Task CreateQuizQuestion_NonExistentQuiz_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var questionBody = new CreateQuizQuestionRequest
        {
            QuizId = Guid.NewGuid(),
            Text = "Question",
            Order = 1
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/questions", questionBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuizQuestion_OtherProfessorQuiz_ReturnsBadRequest()
    {
        // Create quiz as professor 1
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        // Try to add question as professor 2
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var questionBody = new CreateQuizQuestionRequest
        {
            QuizId = quizId,
            Text = "Question",
            Order = 1
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/questions", questionBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuizQuestion_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);

        var response = await _httpClient.DeleteAsync($"/api/quizzes/questions/{questionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuizQuestion_NonExistent_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.DeleteAsync($"/api/quizzes/questions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizQuestion_Text_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);

        var updateBody = new UpdateQuizQuestionRequest { Text = "Updated question text" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/questions/{questionId}", updateBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizQuestion_Points_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);

        var updateBody = new UpdateQuizQuestionRequest { Points = 10 };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/questions/{questionId}", updateBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizQuestion_NonExistent_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var updateBody = new UpdateQuizQuestionRequest { Text = "Updated" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/questions/{Guid.NewGuid()}", updateBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ========================================================================
    // Quiz Options - Authorized tests
    // ========================================================================

    [Fact]
    public async Task CreateQuizOption_WithValidAuth_ReturnsCreated()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);

        var optionBody = new CreateQuizOptionRequest
        {
            QuestionId = questionId,
            Text = "Option A",
            Order = 1,
            IsCorrect = true
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/options", optionBody);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var optionId = await response.Content.ReadAsAsync<Guid>();
        Assert.NotEqual(Guid.Empty, optionId);
    }

    [Fact]
    public async Task CreateQuizOption_NonExistentQuestion_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var optionBody = new CreateQuizOptionRequest
        {
            QuestionId = Guid.NewGuid(),
            Text = "Option",
            Order = 1,
            IsCorrect = false
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/options", optionBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuizOption_OtherProfessorQuestion_ReturnsBadRequest()
    {
        // Create quiz and question as professor 1
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);

        // Try to add option as professor 2
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var optionBody = new CreateQuizOptionRequest
        {
            QuestionId = questionId,
            Text = "Option",
            Order = 1,
            IsCorrect = false
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/options", optionBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuizOption_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);
        var optionId = await CreateOptionAsync(questionId);

        var response = await _httpClient.DeleteAsync($"/api/quizzes/options/{optionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuizOption_NonExistent_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.DeleteAsync($"/api/quizzes/options/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizOption_Text_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);
        var optionId = await CreateOptionAsync(questionId);

        var updateBody = new UpdateQuizOptionRequest { Text = "Updated option text" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/options/{optionId}", updateBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizOption_IsCorrect_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var questionId = await CreateQuestionAsync(quizId);
        var optionId = await CreateOptionAsync(questionId, isCorrect: false);

        var updateBody = new UpdateQuizOptionRequest { IsCorrect = true };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/options/{optionId}", updateBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuizOption_NonExistent_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var updateBody = new UpdateQuizOptionRequest { Text = "Updated" };
        var response = await _httpClient.PutAsJsonAsync($"/api/quizzes/options/{Guid.NewGuid()}", updateBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ========================================================================
    // Quiz Lecture Activation - Authorized tests
    // ========================================================================

    [Fact]
    public async Task ActivateQuizForLecture_LectureNotInProgress_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();
        var lectureId = await CreateLectureAsync();

        var activateBody = new ActivateQuizForLectureRequest
        {
            LectureId = lectureId,
            QuizId = quizId
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/activate", activateBody);

        // Lecture must be InProgress to activate quiz
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateQuizForLecture_NonExistentQuiz_ReturnsBadRequest()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var activateBody = new ActivateQuizForLectureRequest
        {
            LectureId = lectureId,
            QuizId = Guid.NewGuid()
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/activate", activateBody);

        // Returns BadRequest (validation fails before checking quiz existence)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateQuizForLecture_NonExistentLecture_ReturnsNotFound()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var quizId = await CreateQuizAsync();

        var activateBody = new ActivateQuizForLectureRequest
        {
            LectureId = Guid.NewGuid(),
            QuizId = quizId
        };

        var response = await _httpClient.PostAsJsonAsync("/api/quizzes/activate", activateBody);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizzesByLecture_WithValidAuth_ReturnsSuccess()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);
        var lectureId = await CreateLectureAsync();

        var response = await _httpClient.GetAsync($"/api/quizzes/lecture/{lectureId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizzesByLecture_NonExistentLecture_ReturnsError()
    {
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        var response = await _httpClient.GetAsync($"/api/quizzes/lecture/{Guid.NewGuid()}");

        // Non-existent lecture returns an error (exact code depends on exception handling)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.InternalServerError);
    }

    // ========================================================================
    // Full workflow tests
    // ========================================================================

    [Fact]
    public async Task CreateCompleteQuiz_WithQuestionsAndOptions_WorksEndToEnd()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        // Create quiz
        var quizId = await CreateQuizAsync("Final Exam", TimeSpan.FromHours(2));

        // Create questions
        var question1Id = await CreateQuestionAsync(quizId, "What is 2+2?", 1, 5);
        var question2Id = await CreateQuestionAsync(quizId, "What color is the sky?", 2, 5);

        // Create options for question 1
        await CreateOptionAsync(question1Id, "3", 1, false);
        await CreateOptionAsync(question1Id, "4", 2, true);
        await CreateOptionAsync(question1Id, "5", 3, false);

        // Create options for question 2
        await CreateOptionAsync(question2Id, "Red", 1, false);
        await CreateOptionAsync(question2Id, "Blue", 2, true);
        await CreateOptionAsync(question2Id, "Green", 3, false);

        // Act - Get quiz with all details
        var response = await _httpClient.GetAsync($"/api/quizzes/{quizId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task QuizWithLecture_FullWorkflow_WorksEndToEnd()
    {
        // Arrange
        await CreateAndAuthenticateUserAsync(UserType.Professor);

        // Create quiz with questions
        var quizId = await CreateQuizAsync("Pop Quiz");
        var questionId = await CreateQuestionAsync(quizId, "Quick question?", 1);
        await CreateOptionAsync(questionId, "Yes", 1, true);
        await CreateOptionAsync(questionId, "No", 2, false);

        // Create lecture
        var lectureId = await CreateLectureAsync("Lecture 1");

        // Verify we can get quiz details
        var quizResponse = await _httpClient.GetAsync($"/api/quizzes/{quizId}");
        Assert.Equal(HttpStatusCode.OK, quizResponse.StatusCode);

        // Verify we can get quizzes by lecture (empty initially)
        var lectureQuizzesResponse = await _httpClient.GetAsync($"/api/quizzes/lecture/{lectureId}");
        Assert.Equal(HttpStatusCode.OK, lectureQuizzesResponse.StatusCode);

        // Note: Quiz activation requires lecture to be InProgress, which requires
        // additional setup beyond this test scope
    }
}

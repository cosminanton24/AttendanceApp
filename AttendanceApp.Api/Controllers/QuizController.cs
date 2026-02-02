using System.Globalization;
using AttendanceApp.Api.Common;
using AttendanceApp.Api.Common.Requests.Quizzes;
using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Application.Features.Quizzes.ActivateQuizForLecture;
using AttendanceApp.Application.Features.Quizzes.CreateQuiz;
using AttendanceApp.Application.Features.Quizzes.CreateQuizOption;
using AttendanceApp.Application.Features.Quizzes.CreateQuizQuestion;
using AttendanceApp.Application.Features.Quizzes.DeleteQuiz;
using AttendanceApp.Application.Features.Quizzes.DeleteQuizOption;
using AttendanceApp.Application.Features.Quizzes.DeleteQuizQuestion;
using AttendanceApp.Application.Features.Quizzes.GetActiveQuizForLecture;
using AttendanceApp.Application.Features.Quizzes.GetQuizById;
using AttendanceApp.Application.Features.Quizzes.GetQuizInfoBatch;
using AttendanceApp.Application.Features.Quizzes.GetQuizzesByLecture;
using AttendanceApp.Application.Features.Quizzes.GetQuizzesByProfessor;
using AttendanceApp.Application.Features.Quizzes.GetQuizSubmissions;
using AttendanceApp.Application.Features.Quizzes.GetStudentQuizResults;
using AttendanceApp.Application.Features.Quizzes.GetSubmissionCount;
using AttendanceApp.Application.Features.Quizzes.GetUserAnswers;
using AttendanceApp.Application.Features.Quizzes.GetUserSubmission;
using AttendanceApp.Application.Features.Quizzes.SaveUserAnswer;
using AttendanceApp.Application.Features.Quizzes.SubmitQuiz;
using AttendanceApp.Application.Features.Quizzes.UpdateQuiz;
using AttendanceApp.Application.Features.Quizzes.UpdateQuizOption;
using AttendanceApp.Application.Features.Quizzes.UpdateQuizQuestion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizController(IMediator mediator) : ControllerBase
{
    #region Quiz CRUD

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateQuizCommand(userId, request.Name, request.Duration);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("professor/{professorId:guid}")]
    public async Task<IActionResult> GetQuizzesByProfessor(
        [FromRoute] Guid professorId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetQuizzesByProfessorQuery(professorId, page, pageSize, name);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyQuizzes(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var query = new GetQuizzesByProfessorQuery(userId, page, pageSize, name);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("batch")]
    public async Task<IActionResult> GetQuizInfoBatch([FromQuery] Guid[] ids, CancellationToken cancellationToken)
    {
        var query = new GetQuizInfoBatchQuery(ids);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("{quizId:guid}")]
    public async Task<IActionResult> GetQuizById([FromRoute] Guid quizId, CancellationToken cancellationToken)
    {
        var query = new GetQuizByIdQuery(quizId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpDelete("{quizId:guid}")]
    public async Task<IActionResult> DeleteQuiz([FromRoute] Guid quizId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new DeleteQuizCommand(userId, quizId);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPut("{quizId:guid}")]
    public async Task<IActionResult> UpdateQuiz(
        [FromRoute] Guid quizId,
        [FromBody] UpdateQuizRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        TimeSpan? duration = null;
        if (!string.IsNullOrWhiteSpace(request.Duration) && TimeSpan.TryParse(request.Duration, CultureInfo.InvariantCulture, out var parsedDuration))
        {
            duration = parsedDuration;
        }
        var command = new UpdateQuizCommand(userId, quizId, request.Name, duration);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    #endregion

    #region Quiz Questions

    [Authorize]
    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuizQuestion([FromBody] CreateQuizQuestionRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateQuizQuestionCommand(userId, request.QuizId, request.Text, request.Order, request.Points);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpDelete("questions/{questionId:guid}")]
    public async Task<IActionResult> DeleteQuizQuestion([FromRoute] Guid questionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new DeleteQuizQuestionCommand(userId, questionId);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPut("questions/{questionId:guid}")]
    public async Task<IActionResult> UpdateQuizQuestion(
        [FromRoute] Guid questionId,
        [FromBody] UpdateQuizQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdateQuizQuestionCommand(userId, questionId, request.Text, request.Points);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    #endregion

    #region Quiz Options

    [Authorize]
    [HttpPost("options")]
    public async Task<IActionResult> CreateQuizOption([FromBody] CreateQuizOptionRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new CreateQuizOptionCommand(userId, request.QuestionId, request.Text, request.Order, request.IsCorrect);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpDelete("options/{optionId:guid}")]
    public async Task<IActionResult> DeleteQuizOption([FromRoute] Guid optionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new DeleteQuizOptionCommand(userId, optionId);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPut("options/{optionId:guid}")]
    public async Task<IActionResult> UpdateQuizOption(
        [FromRoute] Guid optionId, 
        [FromBody] UpdateQuizOptionRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new UpdateQuizOptionCommand(userId, optionId, request.IsCorrect, request.Text);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    #endregion

    #region Quiz Lecture (Activation)

    [Authorize]
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateQuizForLecture([FromBody] ActivateQuizForLectureRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new ActivateQuizForLectureCommand(userId, request.LectureId, request.QuizId);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("lecture/{lectureId:guid}")]
    public async Task<IActionResult> GetQuizzesByLecture([FromRoute] Guid lectureId, CancellationToken cancellationToken)
    {
        var query = new GetQuizzesByLectureQuery(lectureId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("lecture/{lectureId:guid}/active")]
    public async Task<IActionResult> GetActiveQuizForLecture([FromRoute] Guid lectureId, CancellationToken cancellationToken)
    {
        var query = new GetActiveQuizForLectureQuery(lectureId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("lecture/{lectureId:guid}/student-results")]
    public async Task<IActionResult> GetStudentQuizResults([FromRoute] Guid lectureId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = new GetStudentQuizResultsQuery(userId, lectureId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    #endregion

    #region User Answers

    [Authorize]
    [HttpPost("answer")]
    public async Task<IActionResult> SaveUserAnswer([FromBody] SaveUserAnswerRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new SaveUserAnswerCommand(
            userId,
            request.QuizLectureId,
            request.QuestionId,
            request.OptionId,
            request.Choice);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("quiz-lecture/{quizLectureId:guid}/submit")]
    public async Task<IActionResult> SubmitQuiz([FromRoute] Guid quizLectureId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new SubmitQuizCommand(userId, quizLectureId);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("quiz-lecture/{quizLectureId:guid}/answers")]
    public async Task<IActionResult> GetUserAnswers([FromRoute] Guid quizLectureId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = new GetUserAnswersQuery(userId, quizLectureId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("quiz-lecture/{quizLectureId:guid}/submission")]
    public async Task<IActionResult> GetUserSubmission([FromRoute] Guid quizLectureId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = new GetUserSubmissionQuery(userId, quizLectureId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("quiz-lecture/{quizLectureId:guid}/submissions/count")]
    public async Task<IActionResult> GetSubmissionCount([FromRoute] Guid quizLectureId, CancellationToken cancellationToken)
    {
        var query = new GetSubmissionCountQuery(quizLectureId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("quiz-lecture/{quizLectureId:guid}/submissions")]
    public async Task<IActionResult> GetQuizSubmissions(
        [FromRoute] Guid quizLectureId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetQuizSubmissionsQuery(quizLectureId, page, pageSize, search);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    #endregion
}

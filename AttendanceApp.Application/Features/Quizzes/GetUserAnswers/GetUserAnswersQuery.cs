using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetUserAnswers;

public record GetUserAnswersQuery(
    Guid UserId,
    Guid QuizLectureId) : IRequest<Result<IReadOnlyList<UserAnswerDto>>>;

public record UserAnswerDto(
    Guid Id,
    Guid QuestionId,
    Guid OptionId,
    bool Choice);

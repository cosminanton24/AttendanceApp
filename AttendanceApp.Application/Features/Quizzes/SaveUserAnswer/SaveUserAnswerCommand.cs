using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.SaveUserAnswer;

public record SaveUserAnswerCommand(
    Guid UserId,
    Guid QuizLectureId,
    Guid QuestionId,
    Guid OptionId,
    bool Choice) : IRequest<Result<Guid>>;

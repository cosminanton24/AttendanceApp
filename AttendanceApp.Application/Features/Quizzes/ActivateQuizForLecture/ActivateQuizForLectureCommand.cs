using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.ActivateQuizForLecture;

public record ActivateQuizForLectureCommand(
    Guid ProfessorId,
    Guid LectureId,
    Guid QuizId
) : IRequest<Result<Guid>>;

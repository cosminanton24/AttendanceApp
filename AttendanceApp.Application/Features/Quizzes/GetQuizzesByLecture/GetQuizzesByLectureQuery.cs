using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizzesByLecture;

public record GetQuizzesByLectureQuery(Guid LectureId)
    : IRequest<Result<IReadOnlyList<QuizLectureDto>>>;

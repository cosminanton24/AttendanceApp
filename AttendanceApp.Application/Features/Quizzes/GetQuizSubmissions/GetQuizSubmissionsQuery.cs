using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizSubmissions;

public sealed record GetQuizSubmissionsQuery(
    Guid QuizLectureId,
    int PageNumber = 0,
    int PageSize = 20,
    string? SearchTerm = null
) : IRequest<Result<QuizSubmissionsPageDto>>;

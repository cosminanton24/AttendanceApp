using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizzesByProfessor;

public record GetQuizzesByProfessorQuery(
    Guid ProfessorId,
    int PageNumber,
    int PageSize,
    string? NameFilter = null
) : IRequest<Result<QuizzesPageDto>>;

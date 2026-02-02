using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetActiveQuizForLecture;

public record GetActiveQuizForLectureQuery(Guid LectureId)
    : IRequest<Result<ActiveQuizDto?>>;

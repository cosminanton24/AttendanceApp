using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizzesByProfessor;

public class GetQuizzesByProfessorQueryHandler(IQuizRepository quizRepo, IUserRepository userRepo)
    : IRequestHandler<GetQuizzesByProfessorQuery, Result<QuizzesPageDto>>
{
    public async Task<Result<QuizzesPageDto>> Handle(GetQuizzesByProfessorQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(query.ProfessorId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {query.ProfessorId} found.");

        if (user.Type == UserType.Student)
        {
            throw new ValidationException($"User with id {query.ProfessorId} is not authorized to have quizzes.");
        }

        var total = await quizRepo.GetTotalQuizzesByTeacherAsync(
            query.ProfessorId,
            query.NameFilter,
            cancellationToken);

        var quizzes = await quizRepo.GetQuizzesByTeacherAsync(
            query.ProfessorId,
            query.PageNumber,
            query.PageSize,
            query.NameFilter,
            cancellationToken);

        var quizDtos = quizzes.Select(q => new QuizDto(
            q.Id,
            q.Name,
            q.Duration,
            q.ProfessorId,
            q.CreatedAtUtc,
            q.Questions.Count
        )).ToList();

        return Result<QuizzesPageDto>.Ok(new QuizzesPageDto(quizDtos, total));
    }
}

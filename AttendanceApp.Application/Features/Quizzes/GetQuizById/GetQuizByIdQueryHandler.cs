using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Validation;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizById;

public class GetQuizByIdQueryHandler(IQuizRepository quizRepo)
    : IRequestHandler<GetQuizByIdQuery, Result<QuizDetailDto>>
{
    public async Task<Result<QuizDetailDto>> Handle(GetQuizByIdQuery query, CancellationToken cancellationToken)
    {
        var quiz = await quizRepo.GetByIdWithQuestionsAndOptionsAsync(query.QuizId, cancellationToken);

        if (quiz is null)
        {
            throw new ValidationException($"Quiz with id {query.QuizId} not found.");
        }

        var questionDtos = quiz.Questions
            .OrderBy(q => q.Order)
            .Select(q => new QuizQuestionDto(
                q.Id,
                q.QuizId,
                q.Text,
                q.Order,
                q.Points,
                q.Options
                    .OrderBy(o => o.Order)
                    .Select(o => new QuizOptionDto(
                        o.Id,
                        o.Text,
                        o.Order,
                        o.IsCorrect
                    )).ToList()
            )).ToList();

        var dto = new QuizDetailDto(
            quiz.Id,
            quiz.Name,
            quiz.Duration,
            quiz.ProfessorId,
            quiz.CreatedAtUtc,
            questionDtos
        );

        return Result<QuizDetailDto>.Ok(dto);
    }
}

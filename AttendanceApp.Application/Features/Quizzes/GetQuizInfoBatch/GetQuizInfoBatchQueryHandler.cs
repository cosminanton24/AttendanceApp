using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Quizzes.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.GetQuizInfoBatch;

public sealed class GetQuizInfoBatchQueryHandler(IQuizRepository quizRepo)
    : IRequestHandler<GetQuizInfoBatchQuery, Result<IReadOnlyList<QuizDetailDto>>>
{
    public async Task<Result<IReadOnlyList<QuizDetailDto>>> Handle(GetQuizInfoBatchQuery request, CancellationToken cancellationToken)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            throw new ValidationException("At least one quiz id is required.");

        var distinctIds = request.Ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinctIds.Length == 0)
            throw new ValidationException("At least one quiz id is required.");

        var dtos = new List<QuizDetailDto>(distinctIds.Length);
        var missing = new HashSet<Guid>();

        foreach (var id in distinctIds)
        {
            var quiz = await quizRepo.GetByIdWithQuestionsAndOptionsAsync(id, cancellationToken);
            if (quiz is null)
            {
                missing.Add(id);
                continue;
            }

            var questionDtos = quiz.Questions.Select(q => new QuizQuestionDto(
                q.Id,
                q.QuizId,
                q.Text,
                q.Order,
                q.Points,
                q.Options.Select(o => new QuizOptionDto(
                    o.Id,
                    o.Text,
                    o.Order,
                    o.IsCorrect
                )).ToList()
            )).ToList();

            dtos.Add(new QuizDetailDto(
                quiz.Id,
                quiz.Name,
                quiz.Duration,
                quiz.ProfessorId,
                quiz.CreatedAtUtc,
                questionDtos
            ));
        }

        if (missing.Count > 0)
            throw new KeyNotFoundException($"No quiz(zes) found for id(s): {string.Join(", ", missing)}");

        return Result<IReadOnlyList<QuizDetailDto>>.Ok(dtos);
    }
}

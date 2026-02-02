using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Quizzes.GetUserAnswers;

public class GetUserAnswersQueryHandler(IUserAnswerRepository userAnswerRepo)
    : IRequestHandler<GetUserAnswersQuery, Result<IReadOnlyList<UserAnswerDto>>>
{
    public async Task<Result<IReadOnlyList<UserAnswerDto>>> Handle(
        GetUserAnswersQuery query,
        CancellationToken cancellationToken)
    {
        var answers = await userAnswerRepo.GetByUserAndQuizLectureAsync(
            query.UserId,
            query.QuizLectureId,
            cancellationToken);

        var dtos = answers
            .Select(a => new UserAnswerDto(a.Id, a.QuestionId, a.OptionId, a.Choice))
            .ToList();

        return Result<IReadOnlyList<UserAnswerDto>>.Ok(dtos);
    }
}

using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Application.Features.Quizzes.SubmitQuiz;

public class SubmitQuizCommandHandler(
    IUserAnswerRepository userAnswerRepo,
    IUserSubmissionRepository userSubmissionRepo,
    IQuizLectureRepository quizLectureRepo,
    IQuizRepository quizRepo,
    IUserRepository userRepo)
    : IRequestHandler<SubmitQuizCommand, Result<QuizResultDto>>
{
    public async Task<Result<QuizResultDto>> Handle(SubmitQuizCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with id {command.UserId} found.");

        if (user.Type != UserType.Student)
        {
            throw new ValidationException("Only students can submit quizzes.");
        }

        var quizLecture = await quizLectureRepo.GetByIdAsync(command.QuizLectureId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz lecture with id {command.QuizLectureId} found.");

        // Check if already submitted
        var existingSubmission = await userSubmissionRepo.GetByUserAndQuizLectureAsync(
            command.UserId,
            command.QuizLectureId,
            cancellationToken);

        if (existingSubmission != null)
        {
            // Return existing result
            return Result<QuizResultDto>.Ok(new QuizResultDto(
                Score: existingSubmission.Score,
                MaxScore: existingSubmission.MaxScore,
                CorrectQuestions: 0, // Not stored, but not critical
                TotalQuestions: 0));
        }

        // Get the quiz with all questions and options
        var quiz = await quizRepo.GetByIdWithQuestionsAndOptionsAsync(quizLecture.QuizId, cancellationToken)
            ?? throw new KeyNotFoundException($"No quiz with id {quizLecture.QuizId} found.");

        // Get user's answers for this quiz lecture
        var userAnswers = await userAnswerRepo.GetByUserAndQuizLectureAsync(
            command.UserId,
            command.QuizLectureId,
            cancellationToken);

        // Build a lookup for user's selected options (those with Choice = true)
        var selectedOptionsPerQuestion = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var answer in userAnswers.Where(a => a.Choice))
        {
            if (!selectedOptionsPerQuestion.TryGetValue(answer.QuestionId, out HashSet<Guid>? value))
            {
                value = [];
                selectedOptionsPerQuestion[answer.QuestionId] = value;
            }

            value.Add(answer.OptionId);
        }

        decimal totalScore = 0;
        decimal maxScore = 0;
        int correctQuestions = 0;

        foreach (var question in quiz.Questions)
        {
            var questionPoints = question.Points ?? 1m;
            maxScore += questionPoints;

            // Get the correct option IDs for this question
            var correctOptionIds = question.Options
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .ToHashSet();

            // Get user's selected option IDs for this question
            selectedOptionsPerQuestion.TryGetValue(question.Id, out var userSelectedIds);
            userSelectedIds ??= new HashSet<Guid>();

            // Exact match required - user must select exactly the correct options
            if (correctOptionIds.SetEquals(userSelectedIds))
            {
                totalScore += questionPoints;
                correctQuestions++;
            }
        }

        // Save submission
        var submission = UserSubmission.Create(
            userId: command.UserId,
            quizLectureId: command.QuizLectureId,
            submittedAtUtc: DateTime.UtcNow,
            score: totalScore,
            maxScore: maxScore);

        await userSubmissionRepo.AddAsync(submission, cancellationToken);
        await userSubmissionRepo.SaveChangesAsync(cancellationToken);

        return Result<QuizResultDto>.Ok(new QuizResultDto(
            Score: totalScore,
            MaxScore: maxScore,
            CorrectQuestions: correctQuestions,
            TotalQuestions: quiz.Questions.Count));
    }
}

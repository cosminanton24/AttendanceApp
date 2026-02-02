using AttendanceApp.Domain.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApp.Infrastructure.Persistence.Configurations;

public sealed class UserAnswerConfiguration : IEntityTypeConfiguration<UserAnswer>
{
    public void Configure(EntityTypeBuilder<UserAnswer> builder)
    {
        builder.ToTable("user_answers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.QuizLectureId)
            .IsRequired();

        builder.Property(x => x.QuestionId)
            .IsRequired();

        builder.Property(x => x.OptionId)
            .IsRequired();

        builder.Property(x => x.Choice)
            .IsRequired();

        // Composite unique index to prevent duplicate answers
        builder.HasIndex(x => new { x.UserId, x.QuizLectureId, x.QuestionId, x.OptionId })
            .IsUnique();

        // Index for querying user's answers for a quiz lecture
        builder.HasIndex(x => new { x.UserId, x.QuizLectureId });
    }
}

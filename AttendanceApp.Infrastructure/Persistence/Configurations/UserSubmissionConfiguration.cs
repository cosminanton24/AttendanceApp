using AttendanceApp.Domain.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApp.Infrastructure.Persistence.Configurations;

public sealed class UserSubmissionConfiguration : IEntityTypeConfiguration<UserSubmission>
{
    public void Configure(EntityTypeBuilder<UserSubmission> builder)
    {
        builder.ToTable("user_submissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.QuizLectureId)
            .IsRequired();

        builder.Property(x => x.Submitted)
            .IsRequired();

        builder.Property(x => x.SubmittedAtUtc)
            .IsRequired();

        builder.Property(x => x.Score)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.MaxScore)
            .HasPrecision(18, 2)
            .IsRequired();

        // Unique constraint - one submission per user per quiz lecture
        builder.HasIndex(x => new { x.UserId, x.QuizLectureId })
            .IsUnique();
    }
}

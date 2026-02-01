using AttendanceApp.Domain.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApp.Infrastructure.Persistence.Configurations;

public sealed class QuizLectureConfiguration : IEntityTypeConfiguration<QuizLecture>
{
    public void Configure(EntityTypeBuilder<QuizLecture> builder)
    {
        builder.ToTable("quiz_lectures");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.LectureId)
            .IsRequired();

        builder.Property(x => x.QuizId)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.EndTimeUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.LectureId, x.EndTimeUtc });
    }
}

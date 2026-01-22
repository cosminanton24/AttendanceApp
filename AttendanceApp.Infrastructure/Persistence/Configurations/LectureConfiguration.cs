using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AttendanceApp.Infrastructure.Persistence.Configurations;

public sealed class LectureConfiguration : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> builder)
    {
        builder.ToTable("lectures");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProfessorId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.Duration)
            .IsRequired();

        builder.HasMany<LectureAttendee>("_attendees")
            .WithOne()
            .HasForeignKey(a => a.LectureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(nameof(Lecture.Attendees))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

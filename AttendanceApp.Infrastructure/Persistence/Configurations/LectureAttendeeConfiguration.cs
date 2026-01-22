using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApp.Infrastructure.Persistence.Configurations;

public sealed class LectureAttendeeConfiguration : IEntityTypeConfiguration<LectureAttendee>
{
    public void Configure(EntityTypeBuilder<LectureAttendee> builder)
    {
        builder.ToTable("lecture_attendees");

        builder.HasKey(x => new { x.LectureId, x.UserId });

        builder.Property(x => x.LectureId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasOne<Lecture>()
            .WithMany("_attendees")
            .HasForeignKey(x => x.LectureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
    }
}

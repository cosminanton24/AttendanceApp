using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceApp.Infrastructure.Persistence.Configurations;

public sealed class UserFollowingConfiguration : IEntityTypeConfiguration<UserFollowing>
{
    public void Configure(EntityTypeBuilder<UserFollowing> builder)
    {
        builder.ToTable("user_followings");

        builder.HasKey(x => new { x.FollowedId, x.FollowerId });

        builder.Property(x => x.FollowerId)
            .IsRequired();

        builder.Property(x => x.FollowedId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.FollowedId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Persistence;

public class AttendanceAppDbContext : DbContext
{
    public AttendanceAppDbContext(DbContextOptions<AttendanceAppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Lecture> Lectures { get; set; }
    public DbSet<LectureAttendee> LectureAttendees { get; set; }
    public DbSet<UserFollowing> UserFollowings { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<QuizOption> QuizOptions { get; set; }
    public DbSet<QuizLecture> QuizLectures { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceAppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
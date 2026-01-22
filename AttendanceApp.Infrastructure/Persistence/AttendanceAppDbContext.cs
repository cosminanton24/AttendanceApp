using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Persistence;

public class AttendanceAppDbContext : DbContext
{
    public AttendanceAppDbContext(DbContextOptions<AttendanceAppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Clients { get; set; }
    public DbSet<Lecture> Buildings { get; set; }
    public DbSet<LectureAttendee> Countries { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceAppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
using AttendanceApp.Domain.Enums;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Web.Services;

public sealed class LectureStatusBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<LectureStatusBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EndExpiredLecturesAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EndExpiredLecturesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task EndExpiredLecturesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AttendanceAppDbContext>();
            var now = DateTime.UtcNow;

            var inProgressLectures = await dbContext.Lectures
                .Where(lecture =>
                    lecture.Status == LectureStatus.InProgress &&
                    lecture.StartTime <= now)
                .ToListAsync(cancellationToken);

            var expiredLectures = inProgressLectures
                .Where(lecture => lecture.EndTime <= now)
                .ToList();

            if (expiredLectures.Count == 0)
            {
                return;
            }

            foreach (var lecture in expiredLectures)
            {
                lecture.End();
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Ended {LectureCount} expired in-progress lectures.",
                expiredLectures.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to end expired in-progress lectures.");
        }
    }
}

using AttendanceApp.Application.Features.Lectures.UpdateStatus;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public static class UpdateLectureStatusRequestToCommand
{
    public static UpdateLectureStatusCommand ToCommand(Guid userId, Guid lectureId, UpdateLectureStatusRequest request, string? pos = null)
    {
        return new UpdateLectureStatusCommand(
            userId,
            lectureId,
            request.Status,
            pos
        );
    }
}
using AttendanceApp.Application.Features.Lectures.CreateLecture;
using AttendanceApp.Application.Features.Lectures.UpdateStatus;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public static class UpdateLectureStatusRequestToCommand
{
    public static UpdateLectureStatusCommand ToCommand(Guid userId, Guid lectureId, UpdateLectureStatusRequest request)
    {
        return new UpdateLectureStatusCommand(
            userId,
            lectureId,
            request.Status
        );
    }
}
using AttendanceApp.Application.Features.Lectures.CreateLecture;
using AttendanceApp.Application.Features.Users.CreateUser;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public static class CerateLectureRequestToCommand
{
    public static CreateLectureCommand ToCommand(Guid professorId, CerateLectureRequest request)
    {
        return new CreateLectureCommand(
            professorId,
            request.Name,
            request.Description,
            request.StartTime,
            request.Duration
        );
    }
}
using AttendanceApp.Application.Features.Lectures.CreateLecture;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public static class CerateLectureRequestToCommand
{
    public static CreateLectureCommand ToCommand(Guid professorId, CreateLectureRequest request)
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
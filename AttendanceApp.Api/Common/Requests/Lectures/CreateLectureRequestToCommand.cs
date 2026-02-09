using AttendanceApp.Application.Features.Lectures.CreateLecture;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public static class CreateLectureRequestToCommand
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
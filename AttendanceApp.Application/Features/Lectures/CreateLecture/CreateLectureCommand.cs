using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Lectures.CreateLecture;

public record CreateLectureCommand(Guid ProfessorId, string Name, string Description, DateTime StartTime, TimeSpan Duration) : IRequest<Result<Guid>>;
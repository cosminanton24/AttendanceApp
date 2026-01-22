using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Lectures.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Lectures.GetProfessorLectures;

public record GetProfessorLecturesQuery(Guid ProfessorId, int PageNumber, int PageSize, int? FromMonthsAgo) : IRequest<Result<IReadOnlyList<LectureDto>>>;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Lectures.Dtos;
using AttendanceApp.Domain.Enums;
using MediatR;

namespace AttendanceApp.Application.Features.Lectures.GetStudentLectures;

public record GetStudentLecturesQuery(Guid StudentId, int PageNumber, int PageSize, int? FromMonthsAgo, LectureStatus? Status) : IRequest<Result<IReadOnlyList<LectureDto>>>;
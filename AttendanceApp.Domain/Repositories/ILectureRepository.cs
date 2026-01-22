using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Lectures;

namespace AttendanceApp.Domain.Repositories;

public interface ILectureRepository : IRepository<Lecture>
{
    Task<IReadOnlyList<Lecture>> GetProfessorLecturesAsync(
        Guid professorId, 
        int pageNumber, 
        int pageSize, 
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lecture>> GetStudentLecturesAsync(
        Guid userId,
        int pageNumber, 
        int pageSize,
        DateTime? fromDate = null,
        LectureStatus? status = null,
        CancellationToken cancellationToken = default);
}
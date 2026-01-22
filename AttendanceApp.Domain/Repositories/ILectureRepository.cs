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
}
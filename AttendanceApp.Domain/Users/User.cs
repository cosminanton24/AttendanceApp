using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Enums;

namespace AttendanceApp.Domain.Users;

public class User : AggregateRoot<Guid>
{
    private readonly List<Guid> _attendedLectures = [];

    public UserType Type { get; private set; }

    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Password { get; private set; } = default!;

    public IReadOnlyCollection<Guid> AttendedLectures => _attendedLectures.AsReadOnly();

    private User() { }

    public User(Guid id, UserType type, string name, string email, string password)
    {
        Guard.NotNull(id, nameof(id));
        Guard.NotNullOrWhiteSpace(name, nameof(name), maxLen: 100);
        Guard.NotNullOrWhiteSpace(email, nameof(email), maxLen: 255);
        Guard.NotNullOrWhiteSpace(password, nameof(password), maxLen: 255);

        Id = id;
        Type = type;
        Name = name.Trim();
        Email = email.Trim();
        Password = password;
    }

    public void ChangeName(string name)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name), maxLen: 100);
        Name = name.Trim();
    }

    public void ChangeEmail(string email)
    {
        Guard.NotNullOrWhiteSpace(email, nameof(email), maxLen: 255);
        Email = email.Trim();
    }

    public void ChangePassword(string password)
    {
        Guard.NotNullOrWhiteSpace(password, nameof(password), maxLen: 255);
        Password = password;
    }

    public void ChangeType(UserType type)
    {
        Type = type;
    }
}

using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Users;

namespace AttendanceApp.UnitTests.Domain;

public class UserTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _name = "John Doe";
    private readonly string _email = "john@example.com";
    private readonly string _password = "SecurePassword123";

    [Fact]
    public void Constructor_WithValidData_CreatesUser()
    {
        // Act
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Assert
        Assert.Equal(_userId, user.Id);
        Assert.Equal(UserType.Student, user.Type);
        Assert.Equal(_name, user.Name);
        Assert.Equal(_email, user.Email);
        Assert.Equal(_password, user.Password);
        Assert.Empty(user.AttendedLectures);
    }

    [Fact]
    public void Constructor_TrimsNameAndEmail()
    {
        // Arrange
        var nameWithSpaces = "  John Doe  ";
        var emailWithSpaces = "  john@example.com  ";

        // Act
        var user = new User(_userId, UserType.Professor, nameWithSpaces, emailWithSpaces, _password);

        // Assert
        Assert.Equal("John Doe", user.Name);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public void Constructor_ValidId_CreatesUser()
    {
        // Act
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Assert
        Assert.Equal(_userId, user.Id);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, null!, _email, _password));
        Assert.Equal("name is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, "", _email, _password));
        Assert.Equal("name is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, "   ", _email, _password));
        Assert.Equal("name is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithNameExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, longName, _email, _password));
        Assert.Equal("name must be <= 100 chars.", ex.Message);
    }

    [Fact]
    public void Constructor_WithNullEmail_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, _name, null!, _password));
        Assert.Equal("email is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyEmail_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, _name, "", _password));
        Assert.Equal("email is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmailExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var longEmail = new string('a', 256);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, _name, longEmail, _password));
        Assert.Equal("email must be <= 255 chars.", ex.Message);
    }

    [Fact]
    public void Constructor_WithNullPassword_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, _name, _email, null!));
        Assert.Equal("password is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyPassword_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, _name, _email, ""));
        Assert.Equal("password is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithPasswordExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var longPassword = new string('a', 256);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new User(_userId, UserType.Student, _name, _email, longPassword));
        Assert.Equal("password must be <= 255 chars.", ex.Message);
    }

    [Fact]
    public void Constructor_WithAllUserTypes_CreatesUserSuccessfully()
    {
        // Act
        var student = new User(_userId, UserType.Student, _name, _email, _password);
        var professor = new User(Guid.NewGuid(), UserType.Professor, _name, _email, _password);
        var admin = new User(Guid.NewGuid(), UserType.Admin, _name, _email, _password);

        // Assert
        Assert.Equal(UserType.Student, student.Type);
        Assert.Equal(UserType.Professor, professor.Type);
        Assert.Equal(UserType.Admin, admin.Type);
    }

    [Fact]
    public void ChangeName_WithValidName_UpdatesName()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var newName = "Jane Doe";

        // Act
        user.ChangeName(newName);

        // Assert
        Assert.Equal(newName, user.Name);
    }

    [Fact]
    public void ChangeName_TrimsName()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var newNameWithSpaces = "  Jane Doe  ";

        // Act
        user.ChangeName(newNameWithSpaces);

        // Assert
        Assert.Equal("Jane Doe", user.Name);
    }

    [Fact]
    public void ChangeName_WithNullName_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangeName(null!));
        Assert.Equal("name is required.", ex.Message);
    }

    [Fact]
    public void ChangeName_WithEmptyName_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangeName(""));
        Assert.Equal("name is required.", ex.Message);
    }

    [Fact]
    public void ChangeName_WithNameExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var longName = new string('a', 101);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangeName(longName));
        Assert.Equal("name must be <= 100 chars.", ex.Message);
    }

    [Fact]
    public void ChangeEmail_WithValidEmail_UpdatesEmail()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var newEmail = "jane@example.com";

        // Act
        user.ChangeEmail(newEmail);

        // Assert
        Assert.Equal(newEmail, user.Email);
    }

    [Fact]
    public void ChangeEmail_TrimsEmail()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var newEmailWithSpaces = "  jane@example.com  ";

        // Act
        user.ChangeEmail(newEmailWithSpaces);

        // Assert
        Assert.Equal("jane@example.com", user.Email);
    }

    [Fact]
    public void ChangeEmail_WithNullEmail_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangeEmail(null!));
        Assert.Equal("email is required.", ex.Message);
    }

    [Fact]
    public void ChangeEmail_WithEmptyEmail_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangeEmail(""));
        Assert.Equal("email is required.", ex.Message);
    }

    [Fact]
    public void ChangeEmail_WithEmailExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var longEmail = new string('a', 256);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangeEmail(longEmail));
        Assert.Equal("email must be <= 255 chars.", ex.Message);
    }

    [Fact]
    public void ChangePassword_WithValidPassword_UpdatesPassword()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var newPassword = "NewSecurePassword456";

        // Act
        user.ChangePassword(newPassword);

        // Assert
        Assert.Equal(newPassword, user.Password);
    }

    [Fact]
    public void ChangePassword_WithNullPassword_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangePassword(null!));
        Assert.Equal("password is required.", ex.Message);
    }

    [Fact]
    public void ChangePassword_WithEmptyPassword_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangePassword(""));
        Assert.Equal("password is required.", ex.Message);
    }

    [Fact]
    public void ChangePassword_WithPasswordExceedingMaxLength_ThrowsDomainException()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);
        var longPassword = new string('a', 256);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => user.ChangePassword(longPassword));
        Assert.Equal("password must be <= 255 chars.", ex.Message);
    }

    [Fact]
    public void ChangeType_WithValidType_UpdatesType()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act
        user.ChangeType(UserType.Professor);

        // Assert
        Assert.Equal(UserType.Professor, user.Type);
    }

    [Fact]
    public void ChangeType_ToMultipleTypes_UpdatesCorrectly()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        user.ChangeType(UserType.Professor);
        Assert.Equal(UserType.Professor, user.Type);

        user.ChangeType(UserType.Admin);
        Assert.Equal(UserType.Admin, user.Type);

        user.ChangeType(UserType.Student);
        Assert.Equal(UserType.Student, user.Type);
    }

    [Fact]
    public void Equals_WithSameUserId_ReturnsTrue()
    {
        // Arrange
        var user1 = new User(_userId, UserType.Student, _name, _email, _password);
        var user2 = new User(_userId, UserType.Professor, "Different Name", "different@example.com", "DifferentPassword");

        // Act & Assert
        Assert.Equal(user1, user2);
    }

    [Fact]
    public void Equals_WithDifferentUserId_ReturnsFalse()
    {
        // Arrange
        var user1 = new User(_userId, UserType.Student, _name, _email, _password);
        var user2 = new User(Guid.NewGuid(), UserType.Student, _name, _email, _password);

        // Act & Assert
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void GetHashCode_WithSameUserId_ReturnsSameHashCode()
    {
        // Arrange
        var user1 = new User(_userId, UserType.Student, _name, _email, _password);
        var user2 = new User(_userId, UserType.Professor, "Different Name", "different@example.com", "DifferentPassword");

        // Act & Assert
        Assert.Equal(user1.GetHashCode(), user2.GetHashCode());
    }

    [Fact]
    public void AttendedLectures_IsReadOnly_ReturnsEmptyInitially()
    {
        // Arrange
        var user = new User(_userId, UserType.Student, _name, _email, _password);

        // Act & Assert
        Assert.NotNull(user.AttendedLectures);
        Assert.IsType<IReadOnlyCollection<Guid>>(user.AttendedLectures, exactMatch: false);
        Assert.Empty(user.AttendedLectures);
    }

    [Fact]
    public void Constructor_WithMaxLengthName_CreatesUserSuccessfully()
    {
        // Arrange
        var maxName = new string('a', 100);

        // Act
        var user = new User(_userId, UserType.Student, maxName, _email, _password);

        // Assert
        Assert.Equal(maxName, user.Name);
    }

    [Fact]
    public void Constructor_WithMaxLengthEmail_CreatesUserSuccessfully()
    {
        // Arrange
        var maxEmail = new string('a', 255);

        // Act
        var user = new User(_userId, UserType.Student, _name, maxEmail, _password);

        // Assert
        Assert.Equal(maxEmail, user.Email);
    }

    [Fact]
    public void Constructor_WithMaxLengthPassword_CreatesUserSuccessfully()
    {
        // Arrange
        var maxPassword = new string('a', 255);

        // Act
        var user = new User(_userId, UserType.Student, _name, _email, maxPassword);

        // Assert
        Assert.Equal(maxPassword, user.Password);
    }
}

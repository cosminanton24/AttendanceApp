namespace AttendanceApp.Domain.Common;
 
internal static class Guard
{
    public static void NotNull(object? value, string name)
    {
        if (value is null) throw new DomainException($"{name} is required.");
    }
 
    public static void NotNullOrWhiteSpace(string? value, string name, int? maxLen = null)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required.");
        if (maxLen.HasValue && value.Length > maxLen.Value) throw new DomainException($"{name} must be <= {maxLen.Value} chars.");
    }
 
    public static void InRange(int value, string name, int min, int max)
    {
        if (value < min || value > max) throw new DomainException($"{name} must be between {min} and {max}.");
    }
 
    public static void Positive(decimal value, string name)
    {
        if (value <= 0) throw new DomainException($"{name} must be > 0.");
    }
    public static void Positive(TimeSpan value, string name)
    {
        if (value <= TimeSpan.Zero)
            throw new DomainException($"{name} must be positive.");
    }
 
    public static void NotNegative(decimal value, string name)
    {
        if (value < 0) throw new DomainException($"{name} must be >= 0.");
    }

    public static void NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
            throw new DomainException($"{name} is required.");
    }

    public static void NotInPast(DateTime value, string name)
    {
        if (value < DateTime.UtcNow)
            throw new DomainException($"{name} cannot be in the past.");
    }    
}
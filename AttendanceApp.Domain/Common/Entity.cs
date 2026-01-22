namespace AttendanceApp.Domain.Common;

public abstract class Entity<TId>
        where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    protected Entity(TId id) => Id = id;
    protected Entity() { } 
    public override bool Equals(object? obj)
        => obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => Id.GetHashCode();
}
using MediatR;

namespace BizCore.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : notnull
{
    private readonly List<INotification> _domainEvents = new();
    
    public TId Id { get; protected set; } = default!;
    
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public void AddDomainEvent(INotification eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem)
    {
        _domainEvents.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}

public abstract class AuditableEntity<TId> : Entity<TId> where TId : notnull
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    protected AuditableEntity() : base() { }
    
    protected AuditableEntity(TId id) : base(id) { }
}
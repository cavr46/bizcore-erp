using System;

namespace BizCore.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; }
    public string CreatedBy { get; protected set; } = string.Empty;
    public DateTime? LastModifiedAt { get; protected set; }
    public string? LastModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public virtual void SetCreationInfo(string userId)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = userId;
    }

    public virtual void SetModificationInfo(string userId)
    {
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = userId;
    }

    public virtual void Delete(string userId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = userId;
    }

    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}

public abstract class AuditableEntity<TId> : AuditableEntity
{
    public TId Id { get; protected set; } = default!;

    protected AuditableEntity()
    {
    }

    protected AuditableEntity(TId id) : this()
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not AuditableEntity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id == null || other.Id == null)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id?.GetHashCode()).GetHashCode();
    }
}
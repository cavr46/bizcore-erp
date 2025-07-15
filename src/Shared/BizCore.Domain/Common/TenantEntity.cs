using System;

namespace BizCore.Domain.Common;

public abstract class TenantEntity : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; protected set; }
    public string CompanyCode { get; protected set; } = string.Empty;

    protected TenantEntity()
    {
    }

    protected TenantEntity(Guid tenantId, string companyCode) : this()
    {
        TenantId = tenantId;
        CompanyCode = companyCode;
    }

    public virtual void SetTenant(Guid tenantId, string companyCode)
    {
        TenantId = tenantId;
        CompanyCode = companyCode;
    }
}

public abstract class TenantEntity<TId> : AuditableEntity<TId>, ITenantEntity
{
    public Guid TenantId { get; protected set; }
    public string CompanyCode { get; protected set; } = string.Empty;

    protected TenantEntity()
    {
    }

    protected TenantEntity(TId id, Guid tenantId, string companyCode) : base(id)
    {
        TenantId = tenantId;
        CompanyCode = companyCode;
    }

    public virtual void SetTenant(Guid tenantId, string companyCode)
    {
        TenantId = tenantId;
        CompanyCode = companyCode;
    }
}
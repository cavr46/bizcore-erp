namespace BizCore.Domain.Common;

public interface ITenantEntity
{
    Guid TenantId { get; }
}
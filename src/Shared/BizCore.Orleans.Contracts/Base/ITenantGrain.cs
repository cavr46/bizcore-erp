using Orleans;

namespace BizCore.Orleans.Contracts.Base;

public interface ITenantGrain : IGrainWithGuidKey
{
    ValueTask<Guid> GetTenantId();
}
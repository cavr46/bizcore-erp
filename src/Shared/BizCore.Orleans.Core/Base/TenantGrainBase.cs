using Orleans;
using Orleans.Runtime;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Core.Base;

public abstract class TenantGrainBase<TState> : Grain, ITenantGrain 
    where TState : class, new()
{
    protected readonly IPersistentState<TState> _state;
    protected Guid TenantId { get; private set; }

    protected TenantGrainBase(
        [PersistentState("state", "Default")] IPersistentState<TState> state)
    {
        _state = state;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = this.GetPrimaryKey(out var keyExtension);
        
        if (!string.IsNullOrEmpty(keyExtension))
        {
            if (Guid.TryParse(keyExtension, out var tenantId))
            {
                TenantId = tenantId;
            }
        }
        
        return base.OnActivateAsync(cancellationToken);
    }

    public ValueTask<Guid> GetTenantId()
    {
        return ValueTask.FromResult(TenantId);
    }

    protected async Task SaveStateAsync()
    {
        await _state.WriteStateAsync();
    }
}
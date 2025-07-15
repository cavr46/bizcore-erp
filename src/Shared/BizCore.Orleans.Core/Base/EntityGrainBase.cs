using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Core.Base;

public abstract class EntityGrainBase<TState> : Grain, IEntityGrain<TState> 
    where TState : class, new()
{
    private readonly IPersistentState<TState> _state;

    protected TState State => _state.State;

    protected EntityGrainBase(IPersistentState<TState> state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public virtual Task<TState?> GetAsync()
    {
        return Task.FromResult(_state.State);
    }

    public virtual Task<bool> ExistsAsync()
    {
        return Task.FromResult(_state.RecordExists);
    }

    public virtual async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }

    public virtual Task<DateTime?> GetLastModifiedAsync()
    {
        // This would typically come from the state object
        // For now, return null as a placeholder
        return Task.FromResult<DateTime?>(null);
    }

    protected async Task SaveStateAsync()
    {
        await _state.WriteStateAsync();
    }
}
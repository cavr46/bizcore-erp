using Orleans;
using System;
using System.Threading.Tasks;

namespace BizCore.Orleans.Contracts.Base;

public interface IEntityGrain<TState> : IGrainWithGuidKey where TState : class
{
    Task<TState?> GetAsync();
    Task<bool> ExistsAsync();
    Task DeleteAsync();
    Task<DateTime?> GetLastModifiedAsync();
}
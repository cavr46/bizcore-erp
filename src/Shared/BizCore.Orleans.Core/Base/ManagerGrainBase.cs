using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Core.Base;

public abstract class ManagerGrainBase : Grain, IManagerGrain
{
    public abstract Task<int> GetCountAsync(Guid tenantId);
    public abstract Task<List<Guid>> GetAllIdsAsync(Guid tenantId, int skip = 0, int take = 100);
    public abstract Task<List<T>> SearchAsync<T>(Guid tenantId, string searchTerm, int skip = 0, int take = 50);
}
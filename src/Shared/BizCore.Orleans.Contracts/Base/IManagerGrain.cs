using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BizCore.Orleans.Contracts.Base;

public interface IManagerGrain : IGrainWithIntegerKey
{
    Task<int> GetCountAsync(Guid tenantId);
    Task<List<Guid>> GetAllIdsAsync(Guid tenantId, int skip = 0, int take = 100);
    Task<List<T>> SearchAsync<T>(Guid tenantId, string searchTerm, int skip = 0, int take = 50);
}
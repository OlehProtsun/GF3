using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IBindService : IBaseService<BindModel>
    {
        Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default);
        Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default);
        Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default);
    }
}

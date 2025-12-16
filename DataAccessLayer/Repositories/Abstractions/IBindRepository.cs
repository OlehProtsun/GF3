using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IBindRepository : IBaseRepository<BindModel>
    {
        Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default);
        Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default);

        // головне для твоєї логіки: зберегти (create/update) по Key
        Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default);
    }
}

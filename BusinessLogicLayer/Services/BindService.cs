using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Services
{
    public class BindService : GenericService<BindModel>, IBindService
    {
        private readonly IBindRepository _bindRepo;

        public BindService(IBindRepository bindRepo) : base(bindRepo)
            => _bindRepo = bindRepo;

        public Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)
            => _bindRepo.GetActiveAsync(ct);

        public Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)
            => _bindRepo.GetByKeyAsync(key, ct);

        public Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)
            => _bindRepo.UpsertByKeyAsync(model, ct);
    }
}

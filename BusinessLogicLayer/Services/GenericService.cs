using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class GenericService<TEntity> : IBaseService<TEntity> where TEntity : class
    {
        private readonly IBaseRepository<TEntity> _repo;

        public GenericService(IBaseRepository<TEntity> repo) => _repo = repo;

        public Task<TEntity?> GetAsync(int id, CancellationToken ct = default)
            => _repo.GetByIdAsync(id, ct);

        public Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
            => _repo.GetAllAsync(ct);

        public virtual Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default)
            => _repo.AddAsync(entity, ct); // repo зберігає одразу

        public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default)
            => _repo.UpdateAsync(entity, ct);

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => _repo.DeleteAsync(id, ct);
    }
}

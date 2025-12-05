using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class ContainerService : GenericService<ContainerModel>, IContainerService
    {
        private readonly IContainerRepository _repo;

        public ContainerService(IContainerRepository repo) : base(repo)
        {
            _repo = repo;
        }

        public Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => _repo.GetByValueAsync(value, ct);
    }
}

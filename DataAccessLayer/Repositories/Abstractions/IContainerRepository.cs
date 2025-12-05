using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IContainerRepository : IBaseRepository<ContainerModel>
    {
        Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default);
    }
}

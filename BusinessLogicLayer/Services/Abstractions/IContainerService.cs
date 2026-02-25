using BusinessLogicLayer.Contracts.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IContainerService : IBaseService<ContainerModel>
    {
        Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default);
    }
}

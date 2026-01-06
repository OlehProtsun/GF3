using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IShopRepository : IBaseRepository<ShopModel>
    {
        Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default);
    }
}

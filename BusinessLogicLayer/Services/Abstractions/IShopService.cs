using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IShopService : IBaseService<ShopModel>
    {
        Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default);
    }
}

using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class ShopService : GenericService<ShopModel>, IShopService
    {
        public ShopService(IShopRepository repo) : base(repo) { }
    }
}

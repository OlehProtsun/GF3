using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class ShopRepository : GenericRepository<ShopModel>, IShopRepository
    {
        public ShopRepository(AppDbContext db) : base(db) { }
    }
}

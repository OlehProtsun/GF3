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
    public class AvailabilityMonthRepository : GenericRepository<AvailabilityMonthModel>, IAvailabilityMonthRepository
    {
        public AvailabilityMonthRepository(AppDbContext db) : base(db) { }
    }
}

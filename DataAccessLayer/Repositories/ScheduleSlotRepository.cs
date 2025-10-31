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
    public class ScheduleSlotRepository : GenericRepository<ScheduleSlotModel>, IScheduleSlotRepository
    {
        public ScheduleSlotRepository(AppDbContext db) : base(db) { }
    }
}

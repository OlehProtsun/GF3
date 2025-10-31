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
    public class EmployeeRepository : GenericRepository<EmployeeModel>, IEmployeeRepository
    {
        public EmployeeRepository(AppDbContext db) : base(db) { }
    }
}

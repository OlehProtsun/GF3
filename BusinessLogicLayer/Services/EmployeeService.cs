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
    public class EmployeeService : GenericService<EmployeeModel>, IEmployeeService
    {
        public EmployeeService(IEmployeeRepository repo) : base(repo) { }
    }
}

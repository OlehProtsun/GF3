using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class EmployeeService : GenericService<EmployeeModel>, IEmployeeService
    {
        private readonly IEmployeeRepository _repo;

        public EmployeeService(IEmployeeRepository repo) : base(repo)
            => _repo = repo;

        public override async Task<EmployeeModel> CreateAsync(EmployeeModel entity, CancellationToken ct = default)
        {
            entity.FirstName = (entity.FirstName ?? "").Trim();
            entity.LastName = (entity.LastName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(entity.FirstName))
                throw new ValidationException("Ім'я є обов'язковим.");
            if (string.IsNullOrWhiteSpace(entity.LastName))
                throw new ValidationException("Прізвище є обов'язковим.");
            if (entity.FirstName.Length > 100 || entity.LastName.Length > 100)
                throw new ValidationException("Ім'я/прізвище занадто довгі.");

            return await base.CreateAsync(entity, ct).ConfigureAwait(false);
        }

        public async Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            return await _repo.GetByValueAsync(value, ct).ConfigureAwait(false);
        }

    }
}

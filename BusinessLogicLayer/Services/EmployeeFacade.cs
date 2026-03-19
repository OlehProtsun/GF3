using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services
{
    public sealed class EmployeeFacade : IEmployeeFacade
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeFacade(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default)
            => (await _employeeService.GetAllAsync(ct).ConfigureAwait(false)).Select(Map).ToList();

        public async Task<IReadOnlyList<EmployeeDto>> GetByValueAsync(string value, CancellationToken ct = default)
            => (await _employeeService.GetByValueAsync(value, ct).ConfigureAwait(false)).Select(Map).ToList();

        public async Task<EmployeeDto?> GetAsync(int id, CancellationToken ct = default)
        {
            var model = await _employeeService.GetAsync(id, ct).ConfigureAwait(false);
            return model is null ? null : Map(model);
        }

        public async Task<EmployeeDto> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
        {
            var created = await _employeeService.CreateAsync(Map(request), ct).ConfigureAwait(false);
            return Map(created);
        }

        public Task UpdateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
            => _employeeService.UpdateAsync(Map(request), ct);

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => _employeeService.DeleteAsync(id, ct);

        private static EmployeeDto Map(EmployeeModel model) => new()
        {
            Id = model.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Phone = model.Phone,
            Email = model.Email
        };

        private static EmployeeModel Map(SaveEmployeeRequest request) => new()
        {
            Id = request.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email
        };
    }
}

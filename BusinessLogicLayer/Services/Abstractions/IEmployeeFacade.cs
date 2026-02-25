using BusinessLogicLayer.Contracts.Employees;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IEmployeeFacade
    {
        Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<EmployeeDto>> GetByValueAsync(string value, CancellationToken ct = default);
        Task<EmployeeDto?> GetAsync(int id, CancellationToken ct = default);
        Task<EmployeeDto> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default);
        Task UpdateAsync(SaveEmployeeRequest request, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}

using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Application-facing adapter for employee use cases.
/// Facades translate between transport-oriented request/response contracts and the richer
/// business-layer models, so controllers do not need to know how domain models are shaped.
/// </summary>
public sealed class EmployeeFacade : IEmployeeFacade
{
    private readonly IEmployeeService _employeeService;

    public EmployeeFacade(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default)
        => (await _employeeService.GetAllAsync(ct).ConfigureAwait(false)).Select(MapToDto).ToList();

    public async Task<IReadOnlyList<EmployeeDto>> GetByValueAsync(string value, CancellationToken ct = default)
        => (await _employeeService.GetByValueAsync(value, ct).ConfigureAwait(false)).Select(MapToDto).ToList();

    public async Task<EmployeeDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var model = await _employeeService.GetAsync(id, ct).ConfigureAwait(false);
        return model is null ? null : MapToDto(model);
    }

    public async Task<EmployeeDto> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
    {
        var created = await _employeeService.CreateAsync(MapToModel(request), ct).ConfigureAwait(false);
        return MapToDto(created);
    }

    public Task UpdateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
        => _employeeService.UpdateAsync(MapToModel(request), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _employeeService.DeleteAsync(id, ct);

    public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
        => _employeeService.TryDeleteAsync(id, ct);

    private static EmployeeDto MapToDto(EmployeeModel model) => new()
    {
        Id = model.Id,
        FirstName = model.FirstName,
        LastName = model.LastName,
        Phone = model.Phone,
        Email = model.Email
    };

    private static EmployeeModel MapToModel(SaveEmployeeRequest request) => new()
    {
        Id = request.Id,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Phone = request.Phone,
        Email = request.Email
    };
}

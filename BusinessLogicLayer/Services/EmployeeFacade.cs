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
    private readonly IEmployeeAccountService _employeeAccountService;
    private readonly IEmployeePresenceService _employeePresenceService;

    public EmployeeFacade(
        IEmployeeService employeeService,
        IEmployeeAccountService employeeAccountService,
        IEmployeePresenceService employeePresenceService)
    {
        _employeeService = employeeService;
        _employeeAccountService = employeeAccountService;
        _employeePresenceService = employeePresenceService;
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var employees = await _employeeService.GetAllAsync(ct).ConfigureAwait(false);
        var accountMap = await _employeeAccountService
            .GetByEmployeeIdsAsync(employees.Select(employee => employee.Id), ct)
            .ConfigureAwait(false);
        var onlineStateMap = _employeePresenceService.GetOnlineStates(employees.Select(employee => employee.Id));

        return employees.Select(employee => MapToDto(employee, GetAccount(accountMap, employee.Id), onlineStateMap)).ToList();
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetByValueAsync(string value, CancellationToken ct = default)
    {
        var employees = await _employeeService.GetByValueAsync(value, ct).ConfigureAwait(false);
        var accountMap = await _employeeAccountService
            .GetByEmployeeIdsAsync(employees.Select(employee => employee.Id), ct)
            .ConfigureAwait(false);
        var onlineStateMap = _employeePresenceService.GetOnlineStates(employees.Select(employee => employee.Id));

        return employees.Select(employee => MapToDto(employee, GetAccount(accountMap, employee.Id), onlineStateMap)).ToList();
    }

    public async Task<EmployeeDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var model = await _employeeService.GetAsync(id, ct).ConfigureAwait(false);
        if (model is null)
        {
            return null;
        }

        var account = await _employeeAccountService.GetByEmployeeIdAsync(id, ct).ConfigureAwait(false);
        return MapToDto(model, account, _employeePresenceService.IsEmployeeOnline(id));
    }

    public async Task<EmployeeDto> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
    {
        var created = await _employeeService.CreateAsync(MapToModel(request), ct).ConfigureAwait(false);
        var account = await _employeeAccountService
            .UpsertForEmployeeAsync(created.Id, request.Username, request.Password, ct)
            .ConfigureAwait(false);

        return MapToDto(created, account, false);
    }

    public async Task UpdateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
    {
        await _employeeService.UpdateAsync(MapToModel(request), ct).ConfigureAwait(false);
        await _employeeAccountService
            .UpsertForEmployeeAsync(request.Id, request.Username, request.Password, ct)
            .ConfigureAwait(false);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _employeeService.DeleteAsync(id, ct);

    public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
        => _employeeService.TryDeleteAsync(id, ct);

    private static EmployeeDto MapToDto(
        EmployeeModel model,
        EmployeeAccountModel? account,
        IReadOnlyDictionary<int, bool> onlineStateMap)
        => MapToDto(
            model,
            account,
            onlineStateMap.TryGetValue(model.Id, out var isOnline) && isOnline);

    private static EmployeeDto MapToDto(EmployeeModel model, EmployeeAccountModel? account, bool isOnline) => new()
    {
        Id = model.Id,
        FirstName = model.FirstName,
        LastName = model.LastName,
        Phone = model.Phone,
        Email = model.Email,
        Username = account?.Username,
        HasLoginAccount = account is not null,
        IsOnline = account is not null && isOnline,
        LastLoginAtUtc = account?.LastLoginAtUtc,
    };

    private static EmployeeModel MapToModel(SaveEmployeeRequest request) => new()
    {
        Id = request.Id,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Phone = request.Phone,
        Email = request.Email
    };

    private static EmployeeAccountModel? GetAccount(IReadOnlyDictionary<int, EmployeeAccountModel> accountMap, int employeeId)
        => accountMap.TryGetValue(employeeId, out var account) ? account : null;
}

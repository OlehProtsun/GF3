using BusinessLogicLayer.Contracts.Employees;
using WebApi.Contracts.Employees;

namespace WebApi.Mappers;

/// <summary>
/// Maps employee DTOs and requests between the API layer and the business layer.
/// </summary>
public static class EmployeeMapper
{
    public static WebApi.Contracts.Employees.EmployeeDto ToApiDto(this BusinessLogicLayer.Contracts.Employees.EmployeeDto dto) => new()
    {
        Id = dto.Id,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Phone = dto.Phone,
        Email = dto.Email,
        Username = dto.Username,
        HasLoginAccount = dto.HasLoginAccount,
        IsOnline = dto.IsOnline,
        LastLoginAtUtc = dto.LastLoginAtUtc,
    };

    public static SaveEmployeeRequest ToSaveRequest(this CreateEmployeeRequest request)
        => MapSaveRequest(request.FirstName, request.LastName, request.Phone, request.Email, request.Username, request.Password);

    public static SaveEmployeeRequest ToSaveRequest(this UpdateEmployeeRequest request, int id)
        => MapSaveRequest(request.FirstName, request.LastName, request.Phone, request.Email, request.Username, request.Password, id);

    private static SaveEmployeeRequest MapSaveRequest(
        string firstName,
        string lastName,
        string? phone,
        string? email,
        string? username,
        string? password,
        int id = 0)
        => new()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
            Username = username,
            Password = password,
        };
}

using BusinessLogicLayer.Contracts.Employees;
using WebApi.Contracts.Employees;

namespace WebApi.Mappers;

public static class EmployeeMapper
{
    public static Contracts.Employees.EmployeeDto ToApiDto(this BusinessLogicLayer.Contracts.Employees.EmployeeDto dto) => new()
    {
        Id = dto.Id,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Phone = dto.Phone,
        Email = dto.Email
    };

    public static SaveEmployeeRequest ToSaveRequest(this CreateEmployeeRequest request) => new()
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Phone = request.Phone,
        Email = request.Email
    };

    public static SaveEmployeeRequest ToSaveRequest(this UpdateEmployeeRequest request, int id) => new()
    {
        Id = id,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Phone = request.Phone,
        Email = request.Email
    };
}

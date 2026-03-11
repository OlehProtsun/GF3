using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityBinds;

public sealed class CreateAvailabilityBindRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Value { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

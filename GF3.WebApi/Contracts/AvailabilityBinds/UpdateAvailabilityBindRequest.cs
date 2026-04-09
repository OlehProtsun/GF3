using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityBinds;

/// <summary>
/// Request payload used to update an existing availability bind.
/// </summary>
public sealed class UpdateAvailabilityBindRequest
{
    /// <summary>
    /// Keyboard shortcut string that triggers the bind.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Availability code inserted by the shortcut.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the bind is currently enabled.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

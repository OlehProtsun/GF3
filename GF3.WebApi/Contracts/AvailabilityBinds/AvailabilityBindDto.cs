namespace WebApi.Contracts.AvailabilityBinds;

public sealed class AvailabilityBindDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

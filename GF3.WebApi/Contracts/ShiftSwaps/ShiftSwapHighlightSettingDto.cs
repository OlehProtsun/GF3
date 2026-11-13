namespace WebApi.Contracts.ShiftSwaps;

public sealed class ShiftSwapHighlightSettingDto
{
    public string HighlightColor { get; init; } = "#BBF7D0";
}

public sealed class SaveShiftSwapHighlightSettingRequest
{
    public string? HighlightColor { get; init; }
}

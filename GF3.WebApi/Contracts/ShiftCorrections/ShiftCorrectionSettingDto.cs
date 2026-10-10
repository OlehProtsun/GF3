namespace WebApi.Contracts.ShiftCorrections;

public sealed class ShiftCorrectionSettingDto
{
    public string HighlightColor { get; init; } = "#FDE68A";
}

public sealed class SaveShiftCorrectionSettingRequest
{
    public string? HighlightColor { get; init; }
}

public sealed class ApproveShiftCorrectionRequest
{
    public string? HighlightColor { get; init; }
}

namespace BusinessLogicLayer.Contracts.Models;

public sealed record GraphCellStyleFieldPatchModel(
    int EmployeeId,
    int DayOfMonth,
    bool ChangeBackgroundColor,
    int? ExpectedBackgroundColorArgb,
    int? BackgroundColorArgb,
    bool ChangeTextColor,
    int? ExpectedTextColorArgb,
    int? TextColorArgb);

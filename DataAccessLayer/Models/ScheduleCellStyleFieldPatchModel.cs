namespace DataAccessLayer.Models;

public sealed record ScheduleCellStyleFieldPatchModel(
    int EmployeeId,
    int DayOfMonth,
    bool ChangeBackgroundColor,
    int? ExpectedBackgroundColorArgb,
    int? BackgroundColorArgb,
    bool ChangeTextColor,
    int? ExpectedTextColorArgb,
    int? TextColorArgb);

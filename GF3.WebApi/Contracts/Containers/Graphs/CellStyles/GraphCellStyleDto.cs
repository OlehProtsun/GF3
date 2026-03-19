namespace WebApi.Contracts.Containers.Graphs.CellStyles;

public sealed class GraphCellStyleDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int DayOfMonth { get; set; }
    public int EmployeeId { get; set; }
    public int? BackgroundColorArgb { get; set; }
    public int? TextColorArgb { get; set; }
}

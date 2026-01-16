using DataAccessLayer.Models;
using System.Windows.Media;

namespace WPFApp.ViewModel.Container
{
    public interface IScheduleMatrixStyleProvider
    {
        int CellStyleRevision { get; }
        bool TryBuildCellReference(object? rowData, string? columnName, out ScheduleMatrixCellRef cellRef);
        Brush? GetCellBackgroundBrush(ScheduleMatrixCellRef cellRef);
        Brush? GetCellForegroundBrush(ScheduleMatrixCellRef cellRef);
        bool TryGetCellStyle(ScheduleMatrixCellRef cellRef, out ScheduleCellStyleModel style);
    }
}

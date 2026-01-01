using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private readonly CancellationTokenSource _lifetimeCts = new();
        private bool isEdit;
        private bool isSuccessful;
        private string message = string.Empty;

        private DataTable? _scheduleTable;
        private Dictionary<string, int> _colNameToEmpId = new();
        private object? _oldCellValue;

        private readonly Pen _gridVPen = new(Color.Gainsboro, 1);
        private readonly Pen _gridHPen = new(Color.FromArgb(70, Color.Gray), 1);
        private readonly Pen _conflictPen = new(Color.Red, 2);

        private const string DayCol = "Day";
        private const string ConflictCol = "HasConflict";
        private const string EmptyMark = "-";

        private readonly Dictionary<string, Control> _containerErrorMap;
        private readonly Dictionary<string, Control> _scheduleErrorMap;

        private bool _scheduleRefreshPending;
        private int _rebindRetry;

        private ScheduleModel? _scheduleProfileModel;

        private readonly List<ScheduleSlotModel> _slots = new();
        private readonly List<ScheduleEmployeeModel> _employees = new();
    }
}

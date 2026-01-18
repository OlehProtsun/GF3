using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.View.Shared;

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
        private readonly ScheduleCellStyleResolver _styleResolver = new();
        private readonly Dictionary<(int day, int empId), ScheduleCellStyleModel> _styleLookup = new();
        private ContextMenuStrip? _scheduleStyleMenu;
        private bool _loggedWeekendStyle;

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
        private readonly List<ScheduleCellStyleModel> _cellStyles = new();

        // Schedule info (guna2GroupBox5) collapse/expand
        private bool _scheduleInfoExpanded = false;
        private int _scheduleInfoExpandedHeight;
        private const int ScheduleInfoCollapsedHeight = 43;

        private bool _scheduleNoteExpanded = false;
        private int _scheduleNoteExpandedHeight;
        private int _scheduleNoteCollapsedHeight;

        private bool _scheduleEmployeeExpanded = false;
        private int _scheduleEmployeeExpandedHeight;

        // для “стека” зліва
        private int _scheduleLeftColumnTop;
        private int _scheduleLeftColumnGap;
        private int _scheduleEmployeeColumnGap;

        private bool _scheduleEditTogglesInitialized;

        private sealed class EmployeeListItem
        {
            public int Id { get; init; }
            public string FullName { get; init; } = string.Empty;
        }


    }
}

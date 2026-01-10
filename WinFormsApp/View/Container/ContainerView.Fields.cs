using DataAccessLayer.Models;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private readonly CancellationTokenSource _lifetimeCts = new();
        private bool isEdit;
        private bool isSuccessful;
        private string message = string.Empty;

        private readonly Pen _gridVPen = new(Color.Gainsboro, 1);
        private readonly Pen _gridHPen = new(Color.FromArgb(70, Color.Gray), 1);
        private readonly Pen _conflictPen = new(Color.Red, 2);

        private const string DayCol = "Day";
        private const string ConflictCol = "HasConflict";
        private const string EmptyMark = "-";

        private readonly Dictionary<string, Control> _containerErrorMap;
        private readonly Dictionary<string, Control> _scheduleErrorMap;

        private ScheduleModel? _scheduleProfileModel;

        private readonly List<ScheduleSlotModel> _slots = new();
        private readonly List<ScheduleEmployeeModel> _employees = new();

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

        private sealed class ScheduleBlockUi
        {
            public Guid Id { get; init; }
            public Panel HostPanel { get; init; } = null!;
            public Guna2GroupBox ScheduleGroupBox { get; init; } = null!;
            public Guna2GroupBox AvailabilityGroupBox { get; init; } = null!;
            public Guna2DataGridView SlotGrid { get; init; } = null!;
            public Guna2DataGridView AvailabilityGrid { get; init; } = null!;
            public Guna2Button HideShowButton { get; init; } = null!;
            public Guna2Button CloseButton { get; init; } = null!;
            public Guna2Button SelectButton { get; init; } = null!;
            public Guna2Button? TitleButton { get; init; }
            public Dictionary<string, int> ColNameToEmpId { get; } = new();
            public List<ScheduleSlotModel> Slots { get; } = new();
            public List<ScheduleEmployeeModel> Employees { get; } = new();
            public object? OldCellValue { get; set; }
            public bool AvailabilityPreviewGridConfigured { get; set; }
            public bool RefreshPending { get; set; }
            public int RebindRetry { get; set; }
            public int ExpandedWidth { get; set; }
            public int ExpandedHeight { get; set; }
            public int CollapsedWidth { get; set; }
            public bool IsCollapsed { get; set; }
            public Color DefaultBorderColor { get; set; }
            public Color DefaultCustomBorderColor { get; set; }
            public int ScheduleId { get; set; }
        }

        private readonly Dictionary<Guid, ScheduleBlockUi> _scheduleBlocks = new();
        private readonly List<Guid> _scheduleBlockOrder = new();
        private readonly Dictionary<Guna2DataGridView, ScheduleBlockUi> _scheduleBlocksByGrid = new();
        private Guid? _selectedScheduleBlockId;
        private bool _scheduleBlocksInitialized;
        private const int ScheduleBlockGap = 18;
        // стартуємо там, де стоїть шаблон у Designer (щоб не “заїжджало” під ліві панелі)
        private int ScheduleBlockStartX => guna2GroupBox11.Left; // 432 у дизайнера
        private int ScheduleBlockStartY => guna2GroupBox11.Top;  // 120 у дизайнера
        private readonly Color _scheduleBlockHighlightColor = Color.FromArgb(51, 71, 255);

    }
}

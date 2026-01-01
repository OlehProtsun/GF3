using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;

namespace WinFormsApp.View.Availability
{
    public partial class AvailabilityView
    {
        private readonly CancellationTokenSource _lifetimeCts = new();
        private BindingSource bindsBindingSource = new();
        private readonly HashSet<int> dirtyBindRows = new();

        private readonly DataTable _groupTable = new();
        private readonly Dictionary<int, string> _employeeIdToColumn = new();

        private readonly DataTable _profileGroupTable = new();

        private static readonly KeysConverter KeysConv = new();
        private readonly Dictionary<string, string> _activeBinds = new(StringComparer.OrdinalIgnoreCase);

        private readonly Pen _matrixVPen = new(Color.Gainsboro, 1);
        private readonly Pen _matrixHPen = new(Color.FromArgb(70, Color.Gray), 1);

        private const string DayCol = "DayOfMonth";
        private const string ColBindValue = "colBindValue";
        private const string ColBindKey = "colBindKey";
        private const string ColBindIsActive = "colBindIsActive";

        private bool isEdit;
        private bool isSuccessful;
        private string message = string.Empty;

        private sealed class EmployeeListItem
        {
            public int Id { get; init; }
            public string FullName { get; init; } = string.Empty;
        }
    }
}

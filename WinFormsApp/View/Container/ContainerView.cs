using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView : Form, IContainerView
    {
        private bool isEdit;
        private bool isSuccessful;
        private string message = string.Empty;
        private DataTable? _scheduleTable;
        private Dictionary<string, int> _colNameToEmpId = new();
        private object? _oldCellValue;

        public ContainerView()
        {
            InitializeComponent();
            ConfigureContainerGrid();
            ConfigureScheduleGrid();
            ConfigureSlotGrid();
            AssociateAndRaiseEvents();
            comboStatus.DataSource = Enum.GetValues(typeof(ScheduleStatus));
        }

        #region Container properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ContainerViewModel Mode { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ContainerViewModel CancelTarget { get; set; } = ContainerViewModel.List;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ContainerId { get => (int)numberContainerId.Value; set => numberContainerId.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ContainerName { get => inputContainerName.Text; set => inputContainerName.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string? ContainerNote { get => inputContainerNote.Text; set => inputContainerNote.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string SearchValue { get => inputSearch.Text; set => inputSearch.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public bool IsEdit { get => isEdit; set => isEdit = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public bool IsSuccessful { get => isSuccessful; set => isSuccessful = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string Message { get => message; set => message = value; }
        #endregion

        #region Schedule properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ScheduleViewModel ScheduleMode { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ScheduleViewModel ScheduleCancelTarget { get; set; } = ScheduleViewModel.List;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleId { get => (int)numberScheduleId.Value; set => numberScheduleId.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleContainerId { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleShopId { get => comboShop.SelectedValue is int v ? v : 0; set => comboShop.SelectedValue = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleName { get => inputScheduleName.Text; set => inputScheduleName.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleYear { get => (int)inputYear.Value; set => inputYear.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMonth { get => (int)inputMonth.Value; set => inputMonth.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int SchedulePeoplePerShift { get => (int)inputPeoplePerShift.Value; set => inputPeoplePerShift.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleShift1 { get => inputShift1.Text; set => inputShift1.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleShift2 { get => inputShift2.Text; set => inputShift2.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxHoursPerEmp { get => (int)inputMaxHours.Value; set => inputMaxHours.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxConsecutiveDays { get => (int)inputMaxConsecutiveDays.Value; set => inputMaxConsecutiveDays.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxConsecutiveFull { get => (int)inputMaxConsecutiveFull.Value; set => inputMaxConsecutiveFull.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public int ScheduleMaxFullPerMonth { get => (int)inputMaxFull.Value; set => inputMaxFull.Value = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string? ScheduleComment { get => inputScheduleComment.Text; set => inputScheduleComment.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public ScheduleStatus ScheduleStatus { get => (ScheduleStatus)comboStatus.SelectedItem!; set => comboStatus.SelectedItem = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public string ScheduleSearch { get => inputScheduleSearch.Text; set => inputScheduleSearch.Text = value; }
        public IList<int> SelectedAvailabilityIds => checkedAvailabilities.CheckedItems
            .OfType<AvailabilityMonthModel>()
            .Select(a => a.Id)
            .ToList();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        private IList<ScheduleSlotModel> _slots = new List<ScheduleSlotModel>();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        private IList<ScheduleEmployeeModel> _employees = new List<ScheduleEmployeeModel>();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IList<ScheduleSlotModel> ScheduleSlots
        {
            get => _slots;
            set
            {
                _slots = value ?? new List<ScheduleSlotModel>();
                RefreshScheduleGrid();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IList<ScheduleEmployeeModel> ScheduleEmployees
        {
            get => _employees;
            set
            {
                _employees = value ?? new List<ScheduleEmployeeModel>();
                RefreshScheduleGrid();
            }
        }
        #endregion

        #region Events
        public event Func<CancellationToken, Task>? SearchEvent;
        public event Func<CancellationToken, Task>? AddEvent;
        public event Func<CancellationToken, Task>? EditEvent;
        public event Func<CancellationToken, Task>? DeleteEvent;
        public event Func<CancellationToken, Task>? SaveEvent;
        public event Func<CancellationToken, Task>? CancelEvent;
        public event Func<CancellationToken, Task>? OpenProfileEvent;

        public event Func<CancellationToken, Task>? ScheduleSearchEvent;
        public event Func<CancellationToken, Task>? ScheduleAddEvent;
        public event Func<CancellationToken, Task>? ScheduleEditEvent;
        public event Func<CancellationToken, Task>? ScheduleDeleteEvent;
        public event Func<CancellationToken, Task>? ScheduleSaveEvent;
        public event Func<CancellationToken, Task>? ScheduleCancelEvent;
        public event Func<CancellationToken, Task>? ScheduleOpenProfileEvent;
        public event Func<CancellationToken, Task>? ScheduleGenerateEvent;
        #endregion

        private void RefreshScheduleGrid()
        {
            if (_slots == null || _slots.Count == 0 || _employees == null || _employees.Count == 0)
            {
                slotGrid.DataSource = null;
                return;
            }

            _scheduleTable = BuildScheduleTable(_slots, _employees, out _colNameToEmpId);

            slotGrid.AutoGenerateColumns = true;
            slotGrid.DataSource = _scheduleTable;

            if (slotGrid.Columns.Contains("HasConflict"))
                slotGrid.Columns["HasConflict"].Visible = false;

            if (slotGrid.Columns.Contains("Day"))
                slotGrid.Columns["Day"].ReadOnly = true;
        }


        private DataTable BuildScheduleTable(
            IList<ScheduleSlotModel> slots,
            IList<ScheduleEmployeeModel> employees,
            out Dictionary<string, int> colNameToEmpId)
        {
            colNameToEmpId = new Dictionary<string, int>();

            var table = new DataTable();
            table.Columns.Add("Day", typeof(int));
            table.Columns.Add("HasConflict", typeof(bool));

            foreach (var emp in employees)
            {
                var displayName = $"{emp.Employee.FirstName} {emp.Employee.LastName}";
                var columnName = displayName;
                var suffix = 1;

                while (table.Columns.Contains(columnName))
                {
                    suffix++;
                    columnName = $"{displayName} ({suffix})";
                }

                table.Columns.Add(columnName, typeof(string));

                colNameToEmpId[columnName] = emp.EmployeeId; // 👈 ключове
            }

            if (slots == null || slots.Count == 0)
                return table;

            var days = slots.Select(s => s.DayOfMonth).Distinct().OrderBy(d => d);

            foreach (var day in days)
            {
                var daySlots = slots.Where(s => s.DayOfMonth == day).ToList();
                var row = table.NewRow();

                row["Day"] = day;
                row["HasConflict"] = daySlots.Any(s => s.EmployeeId == null);

                foreach (var kvp in colNameToEmpId)
                {
                    var columnName = kvp.Key;
                    var empId = kvp.Value;

                    var empSlots = daySlots.Where(s => s.EmployeeId == empId).ToList();

                    if (empSlots.Count == 0)
                    {
                        row[columnName] = "-";
                    }
                    else
                    {
                        var mergedIntervals = MergeIntervalsForDisplay(empSlots);

                        row[columnName] = mergedIntervals.Count == 0
                            ? "-"
                            : string.Join(", ", mergedIntervals.Select(i => $"{i.from} - {i.to}"));
                    }
                }

                table.Rows.Add(row);
            }

            return table;
        }

        private static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots)
        {
            // парсимо час + сортуємо
            var list = slots
                .Select(s =>
                {
                    if (!TryParseTime(s.FromTime, out var f) || !TryParseTime(s.ToTime, out var t))
                        return (ok: false, from: default(TimeSpan), to: default(TimeSpan));
                    return (ok: true, from: f, to: t);
                })
                .Where(x => x.ok)
                .Select(x => (x.from, x.to))
                .Distinct()
                .OrderBy(x => x.from)
                .ToList();

            if (list.Count == 0) return new();

            var merged = new List<(TimeSpan from, TimeSpan to)>();
            foreach (var cur in list)
            {
                if (merged.Count == 0)
                {
                    merged.Add(cur);
                    continue;
                }

                var last = merged[^1];

                // якщо перекривається або "впритик" (15:00 == 15:00) → зливаємо
                if (cur.from <= last.to)
                {
                    var newTo = cur.to > last.to ? cur.to : last.to;
                    merged[^1] = (last.from, newTo);
                }
                else
                {
                    merged.Add(cur);
                }
            }

            return merged
                .Select(x => (x.from.ToString(@"hh\:mm"), x.to.ToString(@"hh\:mm")))
                .ToList();
        }

        private void AssociateAndRaiseEvents()
        {
            btnSearch.Click += async (_, __) => { if (SearchEvent != null) await SearchEvent(CancellationToken.None); };
            btnAdd.Click += async (_, __) => { if (AddEvent != null) await AddEvent(CancellationToken.None); };
            btnEdit.Click += async (_, __) => { if (EditEvent != null) await EditEvent(CancellationToken.None); };
            btnDelete.Click += async (_, __) => { if (DeleteEvent != null) await DeleteEvent(CancellationToken.None); };
            btnSave.Click += async (_, __) => { if (SaveEvent != null) await SaveEvent(CancellationToken.None); };
            btnCancel.Click += async (_, __) => { if (CancelEvent != null) await CancelEvent(CancellationToken.None); };
            containerGrid.CellDoubleClick += async (_, __) => { if (OpenProfileEvent != null) await OpenProfileEvent(CancellationToken.None); };
            inputSearch.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter && SearchEvent != null) await SearchEvent(CancellationToken.None); };

            btnScheduleSearch.Click += async (_, __) => { if (ScheduleSearchEvent != null) await ScheduleSearchEvent(CancellationToken.None); };
            btnScheduleAdd.Click += async (_, __) => { if (ScheduleAddEvent != null) await ScheduleAddEvent(CancellationToken.None); };
            btnScheduleEdit.Click += async (_, __) => { if (ScheduleEditEvent != null) await ScheduleEditEvent(CancellationToken.None); };
            btnScheduleDelete.Click += async (_, __) => { if (ScheduleDeleteEvent != null) await ScheduleDeleteEvent(CancellationToken.None); };
            btnScheduleSave.Click += async (_, __) =>
            {
                slotGrid.EndEdit(); // 👈 комітимо редагування клітинки
                if (ScheduleSaveEvent != null)
                    await ScheduleSaveEvent(CancellationToken.None);
            };
            btnScheduleCancel.Click += async (_, __) => { if (ScheduleCancelEvent != null) await ScheduleCancelEvent(CancellationToken.None); };
            btnGenerate.Click += async (_, __) => { if (ScheduleGenerateEvent != null) await ScheduleGenerateEvent(CancellationToken.None); };
            scheduleGrid.CellDoubleClick += async (_, __) => { if (ScheduleOpenProfileEvent != null) await ScheduleOpenProfileEvent(CancellationToken.None); };

            btnCancelProfile.Click += async (_, __) =>
            {
                CancelTarget = ContainerViewModel.List; // 👈 примусово
                if (CancelEvent != null)
                    await CancelEvent(CancellationToken.None);
            };


            btnScheduleProfileCancel.Click += async (_, __) =>
            {
                if (ScheduleCancelEvent != null)
                    await ScheduleCancelEvent(CancellationToken.None);
            };
        }

        public void SetContainerBindingSource(BindingSource containers)
        {
            containerGrid.AutoGenerateColumns = false;
            containerGrid.DataSource = containers;
        }

        public void SetScheduleBindingSource(BindingSource schedules)
        {
            scheduleGrid.AutoGenerateColumns = false;
            scheduleGrid.DataSource = schedules;
        }

        public void SetSlotBindingSource(BindingSource slots)
        {
            slotGrid.AutoGenerateColumns = false;
            slotGrid.DataSource = slots;
        }

        public void SetShopList(IEnumerable<ShopModel> shops)
        {
            comboShop.DataSource = shops.ToList();
            comboShop.DisplayMember = nameof(ShopModel.Name);
            comboShop.ValueMember = nameof(ShopModel.Id);
        }

        public void SetAvailabilityList(IEnumerable<AvailabilityMonthModel> availabilities)
        {
            checkedAvailabilities.Items.Clear();
            foreach (var a in availabilities)
                checkedAvailabilities.Items.Add(a, false);

            checkedAvailabilities.DisplayMember = nameof(AvailabilityMonthModel.Name);
        }

        private void ConfigureContainerGrid()
        {
            containerGrid.AutoGenerateColumns = false;
            containerGrid.Columns.Clear();
            containerGrid.ReadOnly = true;
            containerGrid.AllowUserToAddRows = false;
            containerGrid.AllowUserToDeleteRows = false;
            containerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = nameof(ContainerModel.Name),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            containerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Note",
                DataPropertyName = nameof(ContainerModel.Note),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
        }

        private void ConfigureScheduleGrid()
        {
            scheduleGrid.AutoGenerateColumns = false;
            scheduleGrid.Columns.Clear();
            scheduleGrid.ReadOnly = true;
            scheduleGrid.AllowUserToAddRows = false;
            scheduleGrid.AllowUserToDeleteRows = false;
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = nameof(ScheduleModel.Name),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Shop",
                DataPropertyName = nameof(ScheduleModel.ShopId),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
            });
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Year",
                DataPropertyName = nameof(ScheduleModel.Year)
            });
            scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Month",
                DataPropertyName = nameof(ScheduleModel.Month)
            });
        }

        private void ConfigureSlotGrid()
        {
            slotGrid.AutoGenerateColumns = true;
            slotGrid.Columns.Clear();
            slotGrid.AllowUserToAddRows = false;
            slotGrid.AllowUserToDeleteRows = false;

            slotGrid.CellPainting += SlotGrid_CellPainting;

            slotGrid.CellBeginEdit += SlotGrid_CellBeginEdit;
            slotGrid.CellValidating += SlotGrid_CellValidating;
            slotGrid.CellEndEdit += SlotGrid_CellEndEdit;

            scheduleSlotProfileGrid.AutoGenerateColumns = true;
            scheduleSlotProfileGrid.Columns.Clear();
        }

        private void SlotGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0) return;

            if (slotGrid.Rows[e.RowIndex].DataBoundItem is not DataRowView rowView)
                return;

            if (rowView["HasConflict"] is not bool hasConflict || !hasConflict)
                return;

            e.Handled = true;
            e.PaintBackground(e.ClipBounds, true);
            e.PaintContent(e.ClipBounds);

            var diameter = Math.Min(e.CellBounds.Width, e.CellBounds.Height) - 6;
            var rect = new Rectangle(
                e.CellBounds.Left + (e.CellBounds.Width - diameter) / 2,
                e.CellBounds.Top + (e.CellBounds.Height - diameter) / 2,
                diameter,
                diameter);

            using var pen = new Pen(Color.Red, 2);
            e.Graphics.DrawEllipse(pen, rect);
        }

        public void SwitchToEditMode()
        {
            tabControl.SelectedTab = tabEdit;
            Mode = ContainerViewModel.Edit;
        }

        public void SwitchToListMode()
        {
            tabControl.SelectedTab = tabList;
            Mode = ContainerViewModel.List;
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = ContainerViewModel.Profile;
        }

        public void SwitchToScheduleEditMode()
        {
            tabControl.SelectedTab = tabScheduleEdit;
            ScheduleMode = ScheduleViewModel.Edit;
        }

        public void SwitchToScheduleListMode()
        {
            tabControl.SelectedTab = tabProfile;
            ScheduleMode = ScheduleViewModel.List;
        }

        public void SwitchToScheduleProfileMode()
        {
            tabControl.SelectedTab = tabScheduleProfile;
            ScheduleMode = ScheduleViewModel.Profile;
            ScheduleCancelTarget = ScheduleViewModel.List;
        }

        public void ClearInputs()
        {
            ContainerId = 0;
            ContainerName = string.Empty;
            ContainerNote = string.Empty;
        }

        public void ClearScheduleInputs()
        {
            ScheduleId = 0;
            ScheduleName = string.Empty;
            ScheduleYear = DateTime.Now.Year;
            ScheduleMonth = DateTime.Now.Month;
            SchedulePeoplePerShift = 1;
            ScheduleShift1 = string.Empty;
            ScheduleShift2 = string.Empty;
            ScheduleMaxHoursPerEmp = 1;
            ScheduleMaxConsecutiveDays = 1;
            ScheduleMaxConsecutiveFull = 1;
            ScheduleMaxFullPerMonth = 1;
            ScheduleComment = string.Empty;
            foreach (int i in checkedAvailabilities.CheckedIndices.Cast<int>().ToList())
                checkedAvailabilities.SetItemChecked(i, false);
            ScheduleSlots = new List<ScheduleSlotModel>();
        }

        public void ShowInfo(string text) => MessageBox.Show(text, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        public void ShowError(string text) => MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        public bool Confirm(string text, string? caption = null)
        {
            return MessageBox.Show(text, caption ?? "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(ContainerName): errorProviderContainer.SetError(inputContainerName, kv.Value); break;
                    case nameof(ContainerNote): errorProviderContainer.SetError(inputContainerNote, kv.Value); break;
                }
            }
        }

        public void SetScheduleValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearScheduleValidationErrors();
            foreach (var kv in errors)
            {
                switch (kv.Key)
                {
                    case nameof(ScheduleName): errorProviderSchedule.SetError(inputScheduleName, kv.Value); break;
                    case nameof(ScheduleContainerId): errorProviderSchedule.SetError(comboShop, kv.Value); break;
                    case nameof(ScheduleShopId): errorProviderSchedule.SetError(comboShop, kv.Value); break;
                    case nameof(ScheduleYear): errorProviderSchedule.SetError(inputYear, kv.Value); break;
                    case nameof(ScheduleMonth): errorProviderSchedule.SetError(inputMonth, kv.Value); break;
                    case nameof(SchedulePeoplePerShift): errorProviderSchedule.SetError(inputPeoplePerShift, kv.Value); break;
                    case nameof(ScheduleShift1): errorProviderSchedule.SetError(inputShift1, kv.Value); break;
                    case nameof(ScheduleShift2): errorProviderSchedule.SetError(inputShift2, kv.Value); break;
                    case nameof(ScheduleMaxHoursPerEmp): errorProviderSchedule.SetError(inputMaxHours, kv.Value); break;
                }
            }
        }

        public void ClearValidationErrors()
        {
            errorProviderContainer.Clear();
        }

        public void ClearScheduleValidationErrors()
        {
            errorProviderSchedule.Clear();
        }

        public void SetProfile(ContainerModel model)
        {
            lblContainerName.Text = $"Name: {model.Name}";
            lblContainerNote.Text = $"Note: {model.Note}";
        }

        public void SetScheduleProfile(ScheduleModel model)
        {
            lblScheduleSummary.Text = $"{model.Name} ({model.Year}/{model.Month}) - {model.Shop?.Name}";

            // бажано брати дані, які точно актуальні
            var slots = _slots ?? model.Slots?.ToList() ?? new List<ScheduleSlotModel>();
            var employees = _employees ?? new List<ScheduleEmployeeModel>();

            if (slots.Count > 0 && employees.Count > 0)
            {
                var table = BuildScheduleTable(slots, employees, out _); // 👈 ось тут

                scheduleSlotProfileGrid.AutoGenerateColumns = true;
                scheduleSlotProfileGrid.DataSource = table;
                scheduleSlotProfileGrid.ReadOnly = true;

                if (scheduleSlotProfileGrid.Columns.Contains("HasConflict"))
                    scheduleSlotProfileGrid.Columns["HasConflict"].Visible = false;
            }
            else
            {
                scheduleSlotProfileGrid.DataSource = new BindingSource { DataSource = slots };
            }
        }

        private void SlotGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            _oldCellValue = slotGrid[e.ColumnIndex, e.RowIndex].Value;
        }

        private void SlotGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colName = slotGrid.Columns[e.ColumnIndex].Name;
            if (colName == "Day" || colName == "HasConflict") return;

            var text = (e.FormattedValue?.ToString() ?? "").Trim();

            if (!TryParseIntervals(text, out _, out var error))
            {
                e.Cancel = true;
                ShowError(error ?? "Invalid format. Use: HH:mm - HH:mm");
            }
        }

        private void SlotGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (slotGrid.Rows[e.RowIndex].DataBoundItem is not DataRowView rowView)
                return;

            var colName = slotGrid.Columns[e.ColumnIndex].Name;
            if (colName == "Day" || colName == "HasConflict") return;

            if (!_colNameToEmpId.TryGetValue(colName, out var empId))
                return;

            var day = (int)rowView["Day"];
            var raw = (rowView[colName]?.ToString() ?? "-").Trim();

            if (!TryParseIntervals(raw, out var intervals, out var error))
            {
                rowView[colName] = _oldCellValue?.ToString() ?? "-";
                ShowError(error ?? "Invalid format.");
                return;
            }

            ApplyIntervalsToSlots(day, empId, intervals);

            // нормалізуємо відображення
            rowView[colName] = intervals.Count == 0
                ? "-"
                : string.Join(", ", intervals.Select(i => $"{i.from} - {i.to}"));

            rowView["HasConflict"] = _slots.Any(s => s.DayOfMonth == day && s.EmployeeId == null);
        }

        private static bool TryParseIntervals(
            string? text,
            out List<(string from, string to)> intervals,
            out string? error)
        {
            intervals = new();
            error = null;

            text = (text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text) || text == "-")
                return true;

            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var p in parts)
            {
                var dash = p.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (dash.Length != 2)
                {
                    error = "Format: HH:mm - HH:mm (можна кілька через кому)";
                    return false;
                }

                if (!TryParseTime(dash[0], out var from) || !TryParseTime(dash[1], out var to))
                {
                    error = "Time must be HH:mm (наприклад 09:00 - 14:30)";
                    return false;
                }

                if (from >= to)
                {
                    error = "From must be earlier than To";
                    return false;
                }

                intervals.Add((from.ToString(@"hh\:mm"), to.ToString(@"hh\:mm")));
            }

            // прибираємо дублікати
            intervals = intervals.Distinct().ToList();
            return true;
        }

        private static bool TryParseTime(string s, out TimeSpan t)
        {
            return TimeSpan.TryParseExact(
                s.Trim(),
                new[] { @"h\:mm", @"hh\:mm" },
                CultureInfo.InvariantCulture,
                out t);
        }

        private void ApplyIntervalsToSlots(int day, int empId, List<(string from, string to)> intervals)
        {
            // гарантуємо, що це mutable List
            var list = _slots as List<ScheduleSlotModel>;
            if (list == null)
            {
                list = _slots.ToList();
                _slots = list;
            }

            var removed = list.Where(s => s.DayOfMonth == day && s.EmployeeId == empId).ToList();
            var preservedStatus = removed.FirstOrDefault()?.Status ?? SlotStatus.UNFURNISHED;

            foreach (var r in removed)
                list.Remove(r);

            foreach (var (from, to) in intervals)
            {
                var slotNo = NextFreeSlotNo(list, day, from, to);

                list.Add(new ScheduleSlotModel
                {
                    ScheduleId = ScheduleId,   // при збереженні сервіс все одно перепише ScheduleId :contentReference[oaicite:4]{index=4}
                    DayOfMonth = day,
                    EmployeeId = empId,
                    FromTime = from,
                    ToTime = to,
                    SlotNo = slotNo,
                    Status = preservedStatus
                });
            }
        }

        private static int NextFreeSlotNo(List<ScheduleSlotModel> list, int day, string from, string to)
        {
            var used = list
                .Where(s => s.DayOfMonth == day && s.FromTime == from && s.ToTime == to)
                .Select(s => s.SlotNo)
                .ToHashSet();

            var n = 1;
            while (used.Contains(n)) n++;
            return n;
        }

    }
}

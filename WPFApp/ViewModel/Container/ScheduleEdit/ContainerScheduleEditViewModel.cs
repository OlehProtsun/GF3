/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/








using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.ScheduleEdit.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using WPFApp.MVVM.Core;
using WPFApp.MVVM.Validation;
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Threading;
using WPFApp.Applications.Preview;
using WPFApp.Applications.Matrix.Schedule;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
        : ViewModelBase, INotifyDataErrorInfo, IScheduleMatrixStyleProvider
    {
        private readonly IAvailabilityGroupService _availabilityGroupService;
        private readonly IEmployeeService _employeeService;
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public enum SchedulePaintMode` та контракт його використання у шарі WPFApp.
        /// </summary>
        public enum SchedulePaintMode
        {
            None,
            Background,
            Foreground
        }

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static readonly string DayColumnName = ScheduleMatrixConstants.DayColumnName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly string DayColumnName = ScheduleMatrixConstants.DayColumnName;
        /// <summary>
        /// Визначає публічний елемент `public static readonly string ConflictColumnName = ScheduleMatrixConstants.ConflictColumnName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly string ConflictColumnName = ScheduleMatrixConstants.ConflictColumnName;
        /// <summary>
        /// Визначає публічний елемент `public static readonly string WeekendColumnName = ScheduleMatrixConstants.WeekendColumnName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly string WeekendColumnName = ScheduleMatrixConstants.WeekendColumnName;
        /// <summary>
        /// Визначає публічний елемент `public static readonly string EmptyMark = ScheduleMatrixConstants.EmptyMark;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly string EmptyMark = ScheduleMatrixConstants.EmptyMark;

        
        
        

        
        
        
        
        private readonly ContainerViewModel _owner;

        
        
        private readonly ValidationErrors _validation = new();

        
        
        
        
        
        private readonly Dictionary<string, int> _colNameToEmpId = new();

        
        
        private readonly ScheduleCellStyleStore _cellStyleStore = new();

        
        
        
        private bool _suppressAvailabilityGroupUpdate;

        
        private SchedulePaintMode _activePaintMode = SchedulePaintMode.None;

        private bool _syncingEmployeeSelection;

        

        private bool _isSaveStatusVisible;
        /// <summary>
        /// Визначає публічний елемент `public bool IsSaveStatusVisible` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsSaveStatusVisible
        {
            get => _isSaveStatusVisible;
            set => SetProperty(ref _isSaveStatusVisible, value);
        }

        private UIStatusKind _saveStatus = UIStatusKind.Success;
        /// <summary>
        /// Визначає публічний елемент `public UIStatusKind SaveStatus` та контракт його використання у шарі WPFApp.
        /// </summary>
        public UIStatusKind SaveStatus
        {
            get => _saveStatus;
            set => SetProperty(ref _saveStatus, value);
        }


        
        
        private static readonly ObservableCollection<ScheduleEmployeeModel> EmptyScheduleEmployees = new();
        private static readonly ObservableCollection<ScheduleSlotModel> EmptyScheduleSlots = new();

        
        private int? _lastFillColorArgb;

        
        private int? _lastTextColorArgb;

        
        private Brush? _lastFillBrush;

        
        private Brush? _lastTextBrush;

        
        private ScheduleMatrixCellRef? _selectedCellRef;

        
        private int _totalEmployees;

        
        private string _totalHoursText = "0h 0m";

        
        
        private ScheduleBlockViewModel? _selectedBlock;

        
        private bool _isEdit;

        
        
        private ShopModel? _selectedShop;

        
        private ShopModel? _pendingSelectedShop;

        
        private AvailabilityGroupModel? _selectedAvailabilityGroup;

        
        private AvailabilityGroupModel? _pendingSelectedAvailabilityGroup;

        
        private ScheduleEmployeeModel? _selectedScheduleEmployee;

        
        private readonly HashSet<int> _manualEmployeeIds = new();

        
        private readonly HashSet<int> _availabilityEmployeeIds = new();



        
        private string _shopSearchText = string.Empty;
        private string _availabilitySearchText = string.Empty;
        private string _employeeSearchText = string.Empty;

        
        private DataView _scheduleMatrix = new DataView();

        
        private DataView _availabilityPreviewMatrix = new DataView();

        
        private int _cellStyleRevision;

        
        private readonly Dictionary<int, Brush> _brushCache = new();

        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void Validation_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            
            ErrorsChanged?.Invoke(this, e);
        }

        
        /// <summary>
        /// Визначає публічний елемент `public bool HasErrors => _validation.HasErrors;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool HasErrors => _validation.HasErrors;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public System.Collections.IEnumerable GetErrors(string? propertyName)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public System.Collections.IEnumerable GetErrors(string? propertyName)
            => _validation.GetErrors(propertyName);

        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public bool CanAddBlock => !IsEdit;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool CanAddBlock => !IsEdit;

        
        /// <summary>
        /// Визначає публічний елемент `public string FormTitle => IsEdit ? "Edit Schedule" : "Add Schedule";` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormTitle => IsEdit ? "Edit Schedule" : "Add Schedule";

        
        /// <summary>
        /// Визначає публічний елемент `public string FormSubtitle => IsEdit` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FormSubtitle => IsEdit
            ? "Update schedule details and press Save."
            : "Fill the form and press Save.";

        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleMatrixCellRef> SelectedCellRefs { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleMatrixCellRef> SelectedCellRefs { get; } = new();

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleBlockViewModel> Blocks { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleBlockViewModel> Blocks { get; } = new();

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleBlockViewModel> OpenedSchedules => Blocks;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleBlockViewModel> OpenedSchedules => Blocks;

        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public SchedulePaintMode ActivePaintMode` та контракт його використання у шарі WPFApp.
        /// </summary>
        public SchedulePaintMode ActivePaintMode
        {
            get => _activePaintMode;
            private set => SetProperty(ref _activePaintMode, value);
        }

        
        
        /// <summary>
        /// Визначає публічний елемент `public int? LastFillColorArgb` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int? LastFillColorArgb
        {
            get => _lastFillColorArgb;
            private set
            {
                
                
                
                
                if (SetProperty(ref _lastFillColorArgb, value))
                    LastFillBrush = value.HasValue ? ToBrushCached(value.Value) : null; 
            }
        }

        
        /// <summary>
        /// Визначає публічний елемент `public int? LastTextColorArgb` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int? LastTextColorArgb
        {
            get => _lastTextColorArgb;
            private set
            {
                if (SetProperty(ref _lastTextColorArgb, value))
                    LastTextBrush = value.HasValue ? ToBrushCached(value.Value) : null;
            }
        }

        
        /// <summary>
        /// Визначає публічний елемент `public Brush? LastFillBrush` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Brush? LastFillBrush
        {
            get => _lastFillBrush;
            private set => SetProperty(ref _lastFillBrush, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public Brush? LastTextBrush` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Brush? LastTextBrush
        {
            get => _lastTextBrush;
            private set => SetProperty(ref _lastTextBrush, value);
        }

        
        /// <summary>
        /// Визначає публічний елемент `public ScheduleMatrixCellRef? SelectedCellRef` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleMatrixCellRef? SelectedCellRef
        {
            get => _selectedCellRef;
            set => SetProperty(ref _selectedCellRef, value);
        }

        
        /// <summary>
        /// Визначає публічний елемент `public int TotalEmployees` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int TotalEmployees
        {
            get => _totalEmployees;
            private set => SetProperty(ref _totalEmployees, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string TotalHoursText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string TotalHoursText
        {
            get => _totalHoursText;
            private set => SetProperty(ref _totalHoursText, value);
        }

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public ScheduleBlockViewModel? SelectedBlock` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleBlockViewModel? SelectedBlock
        {
            get => _selectedBlock;
            set
            {
                
                if (SetProperty(ref _selectedBlock, value))
                {
                    
                    
                    OnPropertiesChanged(
                        nameof(ScheduleId),
                        nameof(ScheduleContainerId),
                        nameof(ScheduleShopId),
                        nameof(ScheduleName),
                        nameof(ScheduleYear),
                        nameof(ScheduleMonth),
                        nameof(SchedulePeoplePerShift),
                        nameof(ScheduleShift1),
                        nameof(ScheduleShift2),
                        nameof(ScheduleMaxHoursPerEmp),
                        nameof(ScheduleMaxConsecutiveDays),
                        nameof(ScheduleMaxConsecutiveFull),
                        nameof(ScheduleMaxFullPerMonth),
                        nameof(ScheduleNote),
                        nameof(ScheduleEmployees),
                        nameof(SelectedAvailabilityGroup));

                    
                    OnPropertyChanged(nameof(ActiveSchedule));

                    SyncSelectionFromBlock();
                    RestoreMatricesForSelection();

                    
                    var groupId = SelectedAvailabilityGroup?.Id ?? 0;
                    if (groupId > 0)
                        SafeForget(LoadAvailabilityContextAsync(groupId));

                    RefreshCellStyleMap();

                    
                    SelectedCellRefs.Clear();
                    SelectedCellRef = null;
                    
                    _manualEmployeeIds.Clear();
                    RebindMinHoursEmployeesView();

                }
            }
        }

        
        /// <summary>
        /// Визначає публічний елемент `public ScheduleBlockViewModel? ActiveSchedule` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleBlockViewModel? ActiveSchedule
        {
            get => SelectedBlock;
            set => SelectedBlock = value;
        }

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public bool IsEdit` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsEdit
        {
            get => _isEdit;
            set
            {
                if (SetProperty(ref _isEdit, value))
                    OnPropertiesChanged(nameof(FormTitle), nameof(FormSubtitle), nameof(CanAddBlock));
            }
        }

        
        
        
        
        
        
        
        
        
        
        
        
        

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleId
        {
            get => SelectedBlock?.Model.Id ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.Id == value) return;
                SelectedBlock.Model.Id = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleContainerId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleContainerId
        {
            get => SelectedBlock?.Model.ContainerId ?? 0;
            set
            {
                if (SelectedBlock is null) return;
                if (SelectedBlock.Model.ContainerId == value) return;
                SelectedBlock.Model.ContainerId = value;
                OnPropertyChanged();
            }
        }

        
        
        
        
        
        
        
        

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleShopId` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleShopId
        {
            get => SelectedBlock?.Model.ShopId ?? 0;
            set => SetScheduleValue(
                value,
                m => m.ShopId,
                (m, v) => m.ShopId = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ScheduleName` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleName
        {
            get => SelectedBlock?.Model.Name ?? string.Empty;
            set => SetScheduleValue(
                value,
                m => m.Name ?? string.Empty,
                (m, v) => m.Name = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleYear` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleYear
        {
            get => SelectedBlock?.Model.Year ?? 0;
            set
            {
                if (SetScheduleValue(
                        value,
                        m => m.Year,
                        (m, v) => m.Year = v,
                        invalidateGenerated: true))
                {
                    OnSchedulePeriodChanged();
                }
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMonth` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMonth
        {
            get => SelectedBlock?.Model.Month ?? 0;
            set
            {
                if (SetScheduleValue(
                        value,
                        m => m.Month,
                        (m, v) => m.Month = v,
                        invalidateGenerated: true))
                {
                    OnSchedulePeriodChanged();
                }
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public int SchedulePeoplePerShift` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int SchedulePeoplePerShift
        {
            get => SelectedBlock?.Model.PeoplePerShift ?? 0;
            set => SetScheduleValue(
                value,
                m => m.PeoplePerShift,
                (m, v) => m.PeoplePerShift = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ScheduleShift1` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleShift1
        {
            get => SelectedBlock?.Model.Shift1Time ?? string.Empty;
            set => SetScheduleValue(
                value,
                m => m.Shift1Time ?? string.Empty,
                (m, v) => m.Shift1Time = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public string ScheduleShift2` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleShift2
        {
            get => SelectedBlock?.Model.Shift2Time ?? string.Empty;
            set => SetScheduleValue(
                value,
                m => m.Shift2Time ?? string.Empty,
                (m, v) => m.Shift2Time = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxHoursPerEmp` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxHoursPerEmp
        {
            get => SelectedBlock?.Model.MaxHoursPerEmpMonth ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxHoursPerEmpMonth,
                (m, v) => m.MaxHoursPerEmpMonth = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxConsecutiveDays` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxConsecutiveDays
        {
            get => SelectedBlock?.Model.MaxConsecutiveDays ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxConsecutiveDays,
                (m, v) => m.MaxConsecutiveDays = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxConsecutiveFull` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxConsecutiveFull
        {
            get => SelectedBlock?.Model.MaxConsecutiveFull ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxConsecutiveFull,
                (m, v) => m.MaxConsecutiveFull = v);
        }

        /// <summary>
        /// Визначає публічний елемент `public int ScheduleMaxFullPerMonth` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int ScheduleMaxFullPerMonth
        {
            get => SelectedBlock?.Model.MaxFullPerMonth ?? 0;
            set => SetScheduleValue(
                value,
                m => m.MaxFullPerMonth,
                (m, v) => m.MaxFullPerMonth = v);
        }

        private const int ScheduleNoteMaxLength = 2000;

        /// <summary>
        /// Визначає публічний елемент `public string ScheduleNote` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ScheduleNote
        {
            get => SelectedBlock?.Model.Note ?? string.Empty;
            set {
                var v = value ?? string.Empty;

                if (v.Length > ScheduleNoteMaxLength)
                    v = v.Substring(0, ScheduleNoteMaxLength);

                SetScheduleValue(
                    value,
                    m => m.Note ?? string.Empty,
                    (m, v) => m.Note = v);
                       
            } 
        }

        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleEmployeeModel> ScheduleEmployees` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleEmployeeModel> ScheduleEmployees
            => SelectedBlock?.Employees ?? EmptyScheduleEmployees;

        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ScheduleSlotModel> ScheduleSlots` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ScheduleSlotModel> ScheduleSlots
            => SelectedBlock?.Slots ?? EmptyScheduleSlots;

        
        
        

        private bool IsManualEmployeeId(int employeeId)
            => employeeId > 0 && _manualEmployeeIds.Contains(employeeId);

        private void RebindMinHoursEmployeesView()
        {
            var source = SelectedBlock?.Employees ?? EmptyScheduleEmployees;
            var view = CollectionViewSource.GetDefaultView(source);

            view.Filter = obj =>
            {
                if (obj is not ScheduleEmployeeModel se)
                    return false;

                
                
                var groupId = SelectedAvailabilityGroup?.Id ?? 0;
                if (groupId <= 0)
                    return false;

                return IsAvailabilityEmployee(se.EmployeeId);
            };


            view.Refresh();
            MinHoursEmployeesView = view;
        }


        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<ShopModel> Shops { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<ShopModel> Shops { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<AvailabilityGroupModel> AvailabilityGroups { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<AvailabilityGroupModel> AvailabilityGroups { get; } = new();
        /// <summary>
        /// Визначає публічний елемент `public ObservableCollection<EmployeeModel> Employees { get; } = new();` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ObservableCollection<EmployeeModel> Employees { get; } = new();

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public ShopModel? SelectedShop` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopModel? SelectedShop
        {
            get => _selectedShop;
            set
            {
                
                var oldId = _selectedShop?.Id ?? 0;
                var newId = value?.Id ?? 0;

                
                if (!SetProperty(ref _selectedShop, value))
                    return;

                
                PendingSelectedShop = value;

                
                
                if (_selectionSyncDepth > 0)
                    return;

                
                if (oldId == newId)
                    return;

                
                
                ScheduleShopSelectionChange(newId);
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public ShopModel? PendingSelectedShop` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ShopModel? PendingSelectedShop
        {
            get => _pendingSelectedShop;
            set
            {
                SetProperty(ref _pendingSelectedShop, value);

                ClearValidationErrors(nameof(PendingSelectedShop));
                ClearValidationErrors(nameof(ScheduleShopId));
                ClearValidationErrors(nameof(SelectedShop)); 
            }
        }


        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupModel? SelectedAvailabilityGroup` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityGroupModel? SelectedAvailabilityGroup
        {
            get => _selectedAvailabilityGroup;
            set
            {
                var oldId = _selectedAvailabilityGroup?.Id ?? 0;
                var newId = value?.Id ?? 0;

                if (!SetProperty(ref _selectedAvailabilityGroup, value))
                    return;

                PendingSelectedAvailabilityGroup = value;

                
                if (_suppressAvailabilityGroupUpdate)
                    return;

                if (oldId == newId)
                    return;

                
                if (SelectedBlock == null)
                    return;

                
                if (_selectionSyncDepth > 0)
                    return;

                
                ScheduleAvailabilitySelectionChange(newId);
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public AvailabilityGroupModel? PendingSelectedAvailabilityGroup` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilityGroupModel? PendingSelectedAvailabilityGroup
        {
            get => _pendingSelectedAvailabilityGroup;
            set
            {
                SetProperty(ref _pendingSelectedAvailabilityGroup, value);
                ClearValidationErrors(nameof(PendingSelectedAvailabilityGroup));
            }
        }

        
        
        

        private EmployeeModel? _selectedEmployee;
        /// <summary>
        /// Визначає публічний елемент `public EmployeeModel? SelectedEmployee` та контракт його використання у шарі WPFApp.
        /// </summary>
        public EmployeeModel? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (!SetProperty(ref _selectedEmployee, value))
                    return;

                if (_syncingEmployeeSelection)
                    return;

                try
                {
                    _syncingEmployeeSelection = true;

                    
                    var id = value?.Id ?? 0;
                    ScheduleEmployeeModel? match = null;

                    if (id > 0 && SelectedBlock != null)
                    {
                        foreach (var se in SelectedBlock.Employees)
                        {
                            var seId = se.EmployeeId;
                            var empId = se.Employee?.Id ?? 0;

                            if (seId == id || empId == id)
                            {
                                match = se;
                                break;
                            }
                        }
                    }

                    SelectedScheduleEmployee = match;
                }
                finally
                {
                    _syncingEmployeeSelection = false;
                }
            }
        }

        /// <summary>
        /// Визначає публічний елемент `public ScheduleEmployeeModel? SelectedScheduleEmployee` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ScheduleEmployeeModel? SelectedScheduleEmployee
        {
            get => _selectedScheduleEmployee;
            set
            {
                if (!SetProperty(ref _selectedScheduleEmployee, value))
                    return;

                if (_syncingEmployeeSelection)
                    return;

                try
                {
                    _syncingEmployeeSelection = true;

                    
                    SelectedEmployee = value?.Employee;
                }
                finally
                {
                    _syncingEmployeeSelection = false;
                }
            }
        }


        
        
        

        /// <summary>
        /// Визначає публічний елемент `public string ShopSearchText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string ShopSearchText
        {
            get => _shopSearchText;
            set => SetProperty(ref _shopSearchText, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string AvailabilitySearchText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string AvailabilitySearchText
        {
            get => _availabilitySearchText;
            set => SetProperty(ref _availabilitySearchText, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public string EmployeeSearchText` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string EmployeeSearchText
        {
            get => _employeeSearchText;
            set => SetProperty(ref _employeeSearchText, value);
        }

        
        
        

        /// <summary>
        /// Визначає публічний елемент `public DataView ScheduleMatrix` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView ScheduleMatrix
        {
            get => _scheduleMatrix;
            private set => SetProperty(ref _scheduleMatrix, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public DataView AvailabilityPreviewMatrix` та контракт його використання у шарі WPFApp.
        /// </summary>
        public DataView AvailabilityPreviewMatrix
        {
            get => _availabilityPreviewMatrix;
            private set => SetProperty(ref _availabilityPreviewMatrix, value);
        }

        
        private ICollectionView _minHoursEmployeesView = null!;
        /// <summary>
        /// Визначає публічний елемент `public ICollectionView MinHoursEmployeesView` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ICollectionView MinHoursEmployeesView
        {
            get => _minHoursEmployeesView;
            private set => SetProperty(ref _minHoursEmployeesView, value);
        }


        
        
        

        /// <summary>
        /// Визначає публічний елемент `public int CellStyleRevision` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int CellStyleRevision
        {
            get => _cellStyleRevision;
            private set => SetProperty(ref _cellStyleRevision, value);
        }

        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SaveCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SaveCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand CancelCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand CancelCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand GenerateCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand GenerateCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand AddBlockCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand AddBlockCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SearchShopCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SearchShopCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SearchAvailabilityCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SearchAvailabilityCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand SearchEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand SearchEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand AddEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand AddEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public AsyncRelayCommand RemoveEmployeeCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AsyncRelayCommand RemoveEmployeeCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand<ScheduleMatrixCellRef?> SetCellBackgroundColorCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand<ScheduleMatrixCellRef?> SetCellBackgroundColorCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand<ScheduleMatrixCellRef?> SetCellTextColorCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand<ScheduleMatrixCellRef?> SetCellTextColorCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand<ScheduleMatrixCellRef?> ClearCellFormattingCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand<ScheduleMatrixCellRef?> ClearCellFormattingCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand<ScheduleMatrixCellRef?> ClearSelectedCellStyleCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand<ScheduleMatrixCellRef?> ClearSelectedCellStyleCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand ClearAllScheduleStylesCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand ClearAllScheduleStylesCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand<ScheduleMatrixCellRef?> ApplyLastFillColorCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand<ScheduleMatrixCellRef?> ApplyLastFillColorCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand<ScheduleMatrixCellRef?> ApplyLastTextColorCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand<ScheduleMatrixCellRef?> ApplyLastTextColorCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand PickFillColorCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand PickFillColorCommand { get; }
        /// <summary>
        /// Визначає публічний елемент `public RelayCommand PickTextColorCommand { get; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public RelayCommand PickTextColorCommand { get; }

        
        /// <summary>
        /// Визначає публічний елемент `public event EventHandler? MatrixChanged;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public event EventHandler? MatrixChanged;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerScheduleEditViewModel(ContainerViewModel owner, IAvailabilityGroupService availabilityGroupService,` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerScheduleEditViewModel(ContainerViewModel owner, IAvailabilityGroupService availabilityGroupService,
            IEmployeeService employeeService)
        {
            _availabilityGroupService = availabilityGroupService;
            _employeeService = employeeService;
            _owner = owner;
            _validation.ErrorsChanged += Validation_ErrorsChanged;

            
            
            
            _shopSelectionDebounce = new UiDebouncedAction(_owner.RunOnUiThreadAsync, SelectionDebounceDelay);
            _availabilitySelectionDebounce = new UiDebouncedAction(_owner.RunOnUiThreadAsync, SelectionDebounceDelay);

            
            SaveCommand = new AsyncRelayCommand(SaveWithValidationAsync);

            
            CancelCommand = new AsyncRelayCommand(() => _owner.CancelScheduleAsync());

            GenerateCommand = new AsyncRelayCommand(GenerateWithValidationAsync);

            
            AddBlockCommand = new AsyncRelayCommand(() => _owner.AddScheduleBlockAsync());

            
            SearchShopCommand = new AsyncRelayCommand(() => _owner.SearchScheduleShopsAsync());
            SearchAvailabilityCommand = new AsyncRelayCommand(() => _owner.SearchScheduleAvailabilityAsync());
            SearchEmployeeCommand = new AsyncRelayCommand(() => _owner.SearchScheduleEmployeesAsync());

            
            AddEmployeeCommand = new AsyncRelayCommand(AddEmployeeManualAsync);
            RemoveEmployeeCommand = new AsyncRelayCommand(RemoveEmployeeManualAsync);

            
            RebindMinHoursEmployeesView();

            
            
            SetCellBackgroundColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(SetCellBackgroundColor);
            SetCellTextColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(SetCellTextColor);
            ClearCellFormattingCommand = new RelayCommand<ScheduleMatrixCellRef?>(ClearCellFormatting);
            ClearSelectedCellStyleCommand = new RelayCommand<ScheduleMatrixCellRef?>(ClearSelectedCellStyles);
            ClearAllScheduleStylesCommand = new RelayCommand(ClearAllScheduleStyles);
            ApplyLastFillColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(ApplyLastFillColor);
            ApplyLastTextColorCommand = new RelayCommand<ScheduleMatrixCellRef?>(ApplyLastTextColor);
            PickFillColorCommand = new RelayCommand(PickFillColor);
            PickTextColorCommand = new RelayCommand(PickTextColor);
        }

        
        
        

        
        /// <summary>
        /// Визначає публічний елемент `public Task SelectBlockAsync(ScheduleBlockViewModel block)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Task SelectBlockAsync(ScheduleBlockViewModel block)
            => _owner.SelectScheduleBlockAsync(block);

        
        /// <summary>
        /// Визначає публічний елемент `public Task CloseBlockAsync(ScheduleBlockViewModel block)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Task CloseBlockAsync(ScheduleBlockViewModel block)
            => _owner.CloseScheduleBlockAsync(block);

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void ResetForNew()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void ResetForNew()
        {
            
            Blocks.Clear();
            SelectedBlock = null;

            
            IsEdit = false;

            
            ClearValidationErrors();

            
            ShopSearchText = string.Empty;
            AvailabilitySearchText = string.Empty;
            EmployeeSearchText = string.Empty;

            
            SelectedShop = null;
            SelectedAvailabilityGroup = null;
            SelectedEmployee = null;
            SelectedScheduleEmployee = null;

            
            ScheduleMatrix = new DataView();
            AvailabilityPreviewMatrix = new DataView();

            
            
            _availabilityPreviewKey = null;

            
            _cellStyleStore.Load(Array.Empty<ScheduleCellStyleModel>());
            CellStyleRevision++; 

            
            SelectedCellRef = null;
            SelectedCellRefs.Clear();
            ActivePaintMode = SchedulePaintMode.None;

            
            _brushCache.Clear();

            
            CancelSelectionDebounce();
        }


        /// <summary>
        /// Визначає публічний елемент `public async Task<(IReadOnlyList<EmployeeModel> employees,` та контракт його використання у шарі WPFApp.
        /// </summary>
        public async Task<(IReadOnlyList<EmployeeModel> employees,
                   IReadOnlyList<ScheduleSlotModel> availabilitySlots,
                   bool periodMatched)>
    GetAvailabilityPreviewAsync(
        int availabilityGroupId,
        int scheduleYear,
        int scheduleMonth,
        string? shift1Text,
        string? shift2Text,
        CancellationToken ct = default)
        {
            var (group, members, days) =
                await _availabilityGroupService.LoadFullAsync(availabilityGroupId, ct)
                                               .ConfigureAwait(false);

            
            var empIds = members.Select(m => m.EmployeeId).Distinct().ToHashSet();

            
            var empById = new Dictionary<int, EmployeeModel>();

            foreach (var m in members)
            {
                if (m.Employee != null)
                    empById[m.EmployeeId] = m.Employee;
            }

            if (empById.Count != empIds.Count)
            {
                var all = await _employeeService.GetAllAsync(ct).ConfigureAwait(false);
                foreach (var e in all)
                {
                    if (empIds.Contains(e.Id))
                        empById[e.Id] = e;
                }
            }

            
            foreach (var m in members)
            {
                if (m.Employee == null && empById.TryGetValue(m.EmployeeId, out var e))
                    m.Employee = e;
            }

            var employees = empById.Values
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToList();

            
            var periodMatched = (group.Year == scheduleYear && group.Month == scheduleMonth);
            if (!periodMatched)
                return (employees, Array.Empty<ScheduleSlotModel>(), false);

            
            var shift1 = ParseShift(shift1Text);
            var shift2 = ParseShift(shift2Text);

            var (_, slots) = AvailabilityPreviewBuilder.Build(
                members,
                days,
                shift1,
                shift2,
                ct);

            return (employees, slots, true);
        }

        private static (string from, string to)? ParseShift(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (!AvailabilityCodeParser.TryNormalizeInterval(text, out var normalized))
                return null;

            var parts = normalized.Split('-', 2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return parts.Length == 2 ? (parts[0], parts[1]) : null;
        }

        private void EnsureSelectedScheduleEmployee()
        {
            
            if (SelectedScheduleEmployee != null)
                return;

            if (SelectedBlock is null)
                return;

            var empId = SelectedEmployee?.Id ?? 0;
            if (empId <= 0)
                return;

            
            SelectedScheduleEmployee = SelectedBlock.Employees
                .FirstOrDefault(se => se.EmployeeId == empId || (se.Employee?.Id ?? 0) == empId);
        }

        private CancellationTokenSource? _saveUiPulseCts;

        private CancellationToken ResetSaveUiPulseCts(CancellationToken outer = default)
        {
            _saveUiPulseCts?.Cancel();
            _saveUiPulseCts?.Dispose();

            _saveUiPulseCts = outer.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(outer)
                : new CancellationTokenSource();

            return _saveUiPulseCts.Token;
        }

        private Task ShowSaveWorkingAsync()
            => _owner.RunOnUiThreadAsync(() =>
            {
                SaveStatus = UIStatusKind.Working;
                IsSaveStatusVisible = true;
            });

        private Task HideSaveStatusAsync()
            => _owner.RunOnUiThreadAsync(() => IsSaveStatusVisible = false);

        private async Task ShowSaveSuccessThenAutoHideAsync(CancellationToken ct, int ms = 450)
        {
            await _owner.RunOnUiThreadAsync(() =>
            {
                SaveStatus = UIStatusKind.Success;
                IsSaveStatusVisible = true;
            }).ConfigureAwait(false);

            try
            {
                await Task.Delay(ms, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await HideSaveStatusAsync().ConfigureAwait(false);
        }

        private async Task PulseSaveWorkingSuccessAsync(int ms = 350, CancellationToken ct = default)
        {
            var uiToken = ResetSaveUiPulseCts(ct);

            await ShowSaveWorkingAsync().ConfigureAwait(false);

            
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            try
            {
                await ShowSaveSuccessThenAutoHideAsync(uiToken, ms).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await HideSaveStatusAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HideSaveStatusAsync().ConfigureAwait(false);
                _owner.ShowError(ex.Message);
            }
        }

        private async Task AddEmployeeAndKeepSelectionAsync()
        {
            await _owner.AddScheduleEmployeeAsync().ConfigureAwait(false);

            
            await _owner.RunOnUiThreadAsync(() =>
            {
                EnsureSelectedScheduleEmployee();
                MatrixChanged?.Invoke(this, EventArgs.Empty);
            }).ConfigureAwait(false);
        }

        private async Task RemoveEmployeeSmartAsync()
        {
            
            await _owner.RunOnUiThreadAsync(() => EnsureSelectedScheduleEmployee()).ConfigureAwait(false);

            await _owner.RemoveScheduleEmployeeAsync().ConfigureAwait(false);

            
            await _owner.RunOnUiThreadAsync(() =>
            {
                if (SelectedScheduleEmployee != null && SelectedBlock != null &&
                    !SelectedBlock.Employees.Contains(SelectedScheduleEmployee))
                {
                    SelectedScheduleEmployee = null;
                }

                MatrixChanged?.Invoke(this, EventArgs.Empty);
            }).ConfigureAwait(false);
        }

        
        
        

        private async Task AddEmployeeManualAsync()
        {
            var block = SelectedBlock;
            var empId = SelectedEmployee?.Id ?? 0;

            bool existedBefore = false;
            if (block != null && empId > 0)
            {
                existedBefore = block.Employees.Any(se =>
                    se.EmployeeId == empId || (se.Employee?.Id ?? 0) == empId);
            }

            await _owner.AddScheduleEmployeeAsync().ConfigureAwait(false);

            bool added = false;

            if (empId > 0)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    if (SelectedBlock is null)
                        return;

                    bool existsAfter = SelectedBlock.Employees.Any(se =>
                        se.EmployeeId == empId || (se.Employee?.Id ?? 0) == empId);

                    
                    if (existsAfter)
                    {
                        _manualEmployeeIds.Add(empId);
                        MinHoursEmployeesView?.Refresh();
                    }

                    added = !existedBefore && existsAfter;
                }).ConfigureAwait(false);
            }

            if (added)
                await PulseSaveWorkingSuccessAsync(350).ConfigureAwait(false);
        }
        private async Task RemoveEmployeeManualAsync()
        {
            
            await _owner.RunOnUiThreadAsync(() => EnsureSelectedScheduleEmployee()).ConfigureAwait(false);

            var block = SelectedBlock;
            var empId = SelectedEmployee?.Id
                        ?? SelectedScheduleEmployee?.EmployeeId
                        ?? 0;

            bool existedBefore = false;
            if (block != null && empId > 0)
            {
                existedBefore = block.Employees.Any(se =>
                    se.EmployeeId == empId || (se.Employee?.Id ?? 0) == empId);
            }

            await _owner.RemoveScheduleEmployeeAsync().ConfigureAwait(false);

            bool removed = false;

            if (empId > 0)
            {
                await _owner.RunOnUiThreadAsync(() =>
                {
                    bool existsAfter = SelectedBlock?.Employees.Any(se =>
                        se.EmployeeId == empId || (se.Employee?.Id ?? 0) == empId) == true;

                    removed = existedBefore && !existsAfter;

                    
                    if (removed)
                    {
                        _manualEmployeeIds.Remove(empId);
                        MinHoursEmployeesView?.Refresh();
                    }
                }).ConfigureAwait(false);
            }

            if (removed)
                await PulseSaveWorkingSuccessAsync(350).ConfigureAwait(false);
        }

        private bool IsAvailabilityEmployee(int employeeId)
    => employeeId > 0 && _availabilityEmployeeIds.Contains(employeeId);

        private void SetAvailabilityEmployees(IEnumerable<EmployeeModel> availabilityEmployees)
        {
            _availabilityEmployeeIds.Clear();

            if (availabilityEmployees != null)
            {
                foreach (var e in availabilityEmployees)
                {
                    if (e != null && e.Id > 0)
                        _availabilityEmployeeIds.Add(e.Id);
                }
            }

            
            MinHoursEmployeesView?.Refresh();
        }


    }
}

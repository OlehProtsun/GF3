using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;
using WPFApp.Service;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.ViewModel.Shop
{
    public enum ShopSection
    {
        List,
        Edit,
        Profile
    }

    public sealed class ShopViewModel : ViewModelBase
    {
        private readonly IShopService _shopService;

        private bool _initialized;
        private int? _openedProfileShopId;

        private object _currentSection = null!;
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        private ShopSection _mode = ShopSection.List;
        public ShopSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        public ShopSection CancelTarget { get; private set; } = ShopSection.List;

        public ShopListViewModel ListVm { get; }
        public ShopEditViewModel EditVm { get; }
        public ShopProfileViewModel ProfileVm { get; }

        public ShopViewModel(IShopService shopService)
        {
            _shopService = shopService;

            ListVm = new ShopListViewModel(this);
            EditVm = new ShopEditViewModel(this);
            ProfileVm = new ShopProfileViewModel(this);

            CurrentSection = ListVm;
        }

        public async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_initialized) return;

            _initialized = true;
            await LoadShopsAsync(ct);
        }

        internal async Task SearchAsync(CancellationToken ct = default)
        {
            var term = ListVm.SearchText;
            var list = string.IsNullOrWhiteSpace(term)
                ? await _shopService.GetAllAsync(ct)
                : await _shopService.GetByValueAsync(term, ct);

            ListVm.SetItems(list);
        }

        internal Task StartAddAsync(CancellationToken ct = default)
        {
            EditVm.ResetForNew();
            CancelTarget = ShopSection.List;
            return SwitchToEditAsync();
        }

        internal async Task EditSelectedAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null) return;

            var latest = await _shopService.GetAsync(selected.Id, ct) ?? selected;

            EditVm.SetShop(latest);

            CancelTarget = Mode == ShopSection.Profile
                ? ShopSection.Profile
                : ShopSection.List;

            await SwitchToEditAsync();
        }

        internal async Task SaveAsync(CancellationToken ct = default)
        {
            EditVm.ClearValidationErrors();

            var model = EditVm.ToModel();
            var errors = Validate(model);
            if (errors.Count > 0)
            {
                EditVm.SetValidationErrors(errors);
                return;
            }

            try
            {
                if (EditVm.IsEdit)
                {
                    await _shopService.UpdateAsync(model, ct);
                }
                else
                {
                    var created = await _shopService.CreateAsync(model, ct);
                    EditVm.ShopId = created.Id;
                    model = created;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }

            ShowInfo(EditVm.IsEdit
                ? "Shop updated successfully."
                : "Shop added successfully.");

            await LoadShopsAsync(ct, selectId: model.Id);

            if (CancelTarget == ShopSection.Profile)
            {
                var profileId = _openedProfileShopId ?? model.Id;
                if (profileId > 0)
                {
                    var latest = await _shopService.GetAsync(profileId, ct) ?? model;
                    ProfileVm.SetProfile(latest);
                    ListVm.SelectedItem = latest;
                }

                await SwitchToProfileAsync();
            }
            else
            {
                await SwitchToListAsync();
            }
        }

        internal async Task DeleteSelectedAsync(CancellationToken ct = default)
        {
            var currentId = GetCurrentShopId();
            if (currentId <= 0) return;

            var currentName = Mode == ShopSection.Profile
                ? ProfileVm.Name
                : ListVm.SelectedItem?.Name ?? string.Empty;

            if (!Confirm(string.IsNullOrWhiteSpace(currentName)
                    ? "Delete shop?"
                    : $"Delete {currentName}?"))
            {
                return;
            }

            try
            {
                await _shopService.DeleteAsync(currentId, ct);
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }

            ShowInfo("Shop deleted successfully.");

            await LoadShopsAsync(ct, selectId: null);
            await SwitchToListAsync();
        }

        internal async Task OpenProfileAsync(CancellationToken ct = default)
        {
            var selected = ListVm.SelectedItem;
            if (selected is null) return;

            var latest = await _shopService.GetAsync(selected.Id, ct) ?? selected;

            _openedProfileShopId = latest.Id;
            ProfileVm.SetProfile(latest);
            ListVm.SelectedItem = latest;

            CancelTarget = ShopSection.List;
            await SwitchToProfileAsync();
        }

        internal Task CancelAsync()
        {
            EditVm.ClearValidationErrors();

            return Mode switch
            {
                ShopSection.Edit => CancelTarget == ShopSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),
                _ => SwitchToListAsync()
            };
        }

        private async Task LoadShopsAsync(CancellationToken ct, int? selectId = null)
        {
            var list = await _shopService.GetAllAsync(ct);
            ListVm.SetItems(list);

            if (selectId.HasValue)
                ListVm.SelectedItem = list.FirstOrDefault(s => s.Id == selectId.Value);
        }

        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = ShopSection.List;
            return Task.CompletedTask;
        }

        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = ShopSection.Edit;
            return Task.CompletedTask;
        }

        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = ShopSection.Profile;
            return Task.CompletedTask;
        }

        private int GetCurrentShopId()
        {
            if (Mode == ShopSection.Profile)
                return ProfileVm.ShopId;

            return ListVm.SelectedItem?.Id ?? 0;
        }

        private static Dictionary<string, string> Validate(ShopModel model)
        {
            var map = new Dictionary<string, string>(capacity: 4);

            if (string.IsNullOrWhiteSpace(model.Name))
                map[nameof(model.Name)] = "Name is required.";

            if (string.IsNullOrWhiteSpace(model.Address))
                map[nameof(model.Address)] = "Address is required.";

            return map;
        }

        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        internal void ShowError(Exception ex)
        {
            var (summary, details) = ExceptionMessageBuilder.Build(ex);
            CustomMessageBox.Show("Error", summary, CustomMessageBoxIcon.Error, okText: "OK", details: details);
        }

        private bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");
    }
}

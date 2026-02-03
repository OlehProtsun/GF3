using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using WPFApp.Infrastructure.Validation;
using WPFApp.Service;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// Частина ViewModel, яка відповідає ТІЛЬКИ за валідацію і помилки.
    ///
    /// Важливо:
    /// - ViewModel реалізує INotifyDataErrorInfo.
    /// - Але фактичне зберігання помилок ми винесли в окремий клас ValidationErrors.
    /// - У головному файлі VM вже є поле:
    ///     private readonly ValidationErrors _validation = new();
    ///   і проксі:
    ///     ErrorsChanged / HasErrors / GetErrors
    ///
    /// Тут ми додаємо “операції”:
    /// - Clear всіх помилок
    /// - Clear помилок для конкретної властивості
    /// - Add помилки до конкретної властивості
    /// - SetMany (пакет помилок, наприклад з сервера)
    ///
    /// Навіщо це окремим файлом:
    /// - щоб VM-файл не роздувався
    /// - щоб не було дублю логіки (Dictionary _errors більше не потрібен)
    /// - щоб будь-який код у VM додавав/чистив помилки ОДНАКОВО
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        /// <summary>
        /// Очистити ВСІ помилки валідації у формі.
        ///
        /// Коли викликати:
        /// - при ResetForNew()
        /// - коли користувач відкрив інший блок/форму і старі помилки вже не актуальні
        ///
        /// Що відбувається всередині:
        /// 1) _validation.ClearAll() прибирає всі записи
        /// 2) ValidationErrors сам підніме ErrorsChanged для кожної властивості,
        ///    щоб WPF прибрав червоні рамки/підказки.
        /// 3) OnPropertyChanged(nameof(HasErrors)) — щоб елементи UI,
        ///    які дивляться на HasErrors, теж оновились.
        /// </summary>
        public void ClearValidationErrors()
        {
            _validation.ClearAll();

            // HasErrors — це computed property, тому WPF не завжди “сам” оновить його.
            // Надійніше явно повідомити, що HasErrors міг змінитись.
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Очистити помилки лише для ОДНІЄЇ властивості.
        ///
        /// Приклад:
        /// - користувач змінив ScheduleName -> ми прибираємо помилку саме ScheduleName
        ///
        /// Викликається в SetScheduleValue(...) у твоєму VM:
        ///     ClearValidationErrors(propertyName)
        /// :contentReference[oaicite:2]{index=2}
        /// </summary>
        private void ClearValidationErrors(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            _validation.Clear(propertyName);
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Додати 1 повідомлення-помилку для конкретної властивості.
        ///
        /// Приклад:
        ///     AddValidationError(nameof(ScheduleYear), "Year is required")
        ///
        /// Важливі нюанси:
        /// - ValidationErrors не додає дублікати (однаковий message вдруге не запише)
        /// - після Add(...) піднімається ErrorsChanged -> WPF одразу покаже помилку біля поля
        /// </summary>
        private void AddValidationError(string propertyName, string message)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            _validation.Add(propertyName, message);
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Встановити набір помилок “пакетом”.
        ///
        /// Коли корисно:
        /// - сервер повернув помилки після Save (наприклад: Name пустий, Month невалідний)
        /// - ти хочеш одним викликом замінити ВСІ старі помилки на нові
        ///
        /// Формат:
        /// - ключ: ім’я властивості (nameof(ScheduleName), nameof(ScheduleMonth) ...)
        /// - значення: текст помилки
        ///
        /// Що робить:
        /// 1) ClearAll
        /// 2) Add по кожній парі
        /// 3) піднімає ErrorsChanged (WPF оновиться)
        /// </summary>
        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            if (errors is null || errors.Count == 0)
            {
                ClearValidationErrors();
                return;
            }

            _validation.SetMany(errors);
            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Зручний helper: якщо хочеш швидко додати помилку “під поточну властивість”
        /// без nameof(...).
        ///
        /// Використання:
        ///     AddValidationError("message");
        ///
        /// propertyName автоматично підставиться як ім’я методу/властивості, звідки виклик.
        /// </summary>
        //private void AddValidationError(string message, [CallerMemberName] string? propertyName = null)
        //{
        //    if (string.IsNullOrWhiteSpace(propertyName))
        //        return;

        //    AddValidationError(propertyName, message);
        //}


        /// <summary>
        /// Перевіряє, чи можна зараз виконувати “велику дію”, наприклад:
        /// - Save (зберегти розклад)
        /// - Generate (згенерувати слоти)
        ///
        /// Ідея така:
        /// 1) Inline-валидація (ValidateProperty) ловить помилки “по одній” під час вводу,
        ///    але користувач може:
        ///    - нічого не чіпати (і помилки ще не прораховані),
        ///    - вставити значення через інший шлях,
        ///    - або стан моделі змінився непрямо (наприклад через selection/команди).
        ///
        /// 2) Тому перед Save/Generate ми робимо “повну” валідацію всієї моделі:
        ///    - збираємо всі помилки одним списком
        ///    - записуємо їх у _validation (через SetValidationErrors)
        ///    - повертаємо true/false: можна чи не можна продовжувати.
        ///
        /// Повертає:
        /// - true  => помилок немає, можна продовжувати (Save/Generate)
        /// - false => є помилки, UI покаже їх, дію треба зупинити
        /// </summary>
        private bool ValidateBeforeSave(bool showDialog = true)
        {
            ApplyPendingSelectionsForValidation();

            // 1) No block -> cannot generate/save
            if (SelectedBlock?.Model is not ScheduleModel model)
            {
                ClearValidationErrors();

                if (showDialog)
                {
                    CustomMessageBox.Show(
                        "Validation",
                        "Please add or select a schedule block first.",
                        CustomMessageBoxIcon.Error,
                        okText: "OK");
                }

                return false;
            }

            // 2) Base validation
            var raw = ScheduleValidationRules.ValidateAll(model);

            // 3) Map keys to VM properties (safe even if already VM keys)
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in raw)
            {
                var vmKey = MapValidationKeyToVm(kv.Key);
                if (!errors.ContainsKey(vmKey))
                    errors[vmKey] = kv.Value;
            }

            // 4) Extra: Shop / Availability (binds to Pending*)
            if (model.ShopId <= 0)
                errors[nameof(PendingSelectedShop)] = "Please select a shop.";

            if (SelectedBlock.SelectedAvailabilityGroupId <= 0)
                errors[nameof(PendingSelectedAvailabilityGroup)] = "Please select an availability group.";

            SetValidationErrors(errors);

            if (showDialog && HasErrors)
            {
                CustomMessageBox.Show(
                    "Validation",
                    BuildValidationSummary(errors),
                    CustomMessageBoxIcon.Error,
                    okText: "OK");
            }

            return !HasErrors;
        }
        /// Перед валідацією/генерацією важливо “зафіксувати” pending selections,
        /// щоб в моделі були актуальні ShopId/AvailabilityGroupId.
        /// Це захищає від ситуацій, коли користувач швидко натискає Generate/Save
        /// ще до спрацювання debounce.
        /// </summary>
        private void ApplyPendingSelectionsForValidation()
        {
            if (SelectedBlock is null)
                return;

            using var _ = EnterSelectionSync();

            if (!ReferenceEquals(PendingSelectedShop, SelectedShop))
                SelectedShop = PendingSelectedShop;

            if (!ReferenceEquals(PendingSelectedAvailabilityGroup, SelectedAvailabilityGroup))
                SelectedAvailabilityGroup = PendingSelectedAvailabilityGroup;

            var pendingShopId = PendingSelectedShop?.Id ?? 0;
            if (ScheduleShopId != pendingShopId)
                ScheduleShopId = pendingShopId;

            var pendingGroupId = PendingSelectedAvailabilityGroup?.Id ?? 0;
            if (SelectedBlock.SelectedAvailabilityGroupId != pendingGroupId)
            {
                SelectedBlock.SelectedAvailabilityGroupId = pendingGroupId;
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);
            }
        }

        /// <summary>
        /// Обгортка над Save, яка спочатку запускає повну валідацію.
        /// Якщо є помилки — ми НЕ викликаємо _owner.SaveScheduleAsync().
        /// </summary>
        private Task SaveWithValidationAsync()
        {
            if (!ValidateBeforeSave(showDialog: true))
                return Task.CompletedTask;

            return _owner.SaveScheduleAsync();
        }                 /// Обгортка над Generate, яка спочатку запускає повну валідацію.
                          /// Якщо є помилки — генерацію не запускаємо.
                          /// </summary>
        private async Task GenerateWithValidationAsync()
        {
            if (!ValidateBeforeSave(showDialog: true))
                return;

            await _owner.GenerateScheduleAsync().ConfigureAwait(false);

            // гарантовано оновити матрицю після генерації (щоб грід підхопив)
            await RefreshScheduleMatrixAsync().ConfigureAwait(false);
        }

        private static string MapValidationKeyToVm(string key)
        {
            // якщо твої rules вже повертають "ScheduleName" то мап не заважає (піде в default)
            return key switch
            {
                nameof(ScheduleModel.Name) => nameof(ScheduleName),
                nameof(ScheduleModel.Year) => nameof(ScheduleYear),
                nameof(ScheduleModel.Month) => nameof(ScheduleMonth),
                nameof(ScheduleModel.PeoplePerShift) => nameof(SchedulePeoplePerShift),
                nameof(ScheduleModel.Shift1Time) => nameof(ScheduleShift1),
                nameof(ScheduleModel.Shift2Time) => nameof(ScheduleShift2),
                nameof(ScheduleModel.MaxHoursPerEmpMonth) => nameof(ScheduleMaxHoursPerEmp),
                nameof(ScheduleModel.MaxConsecutiveDays) => nameof(ScheduleMaxConsecutiveDays),
                nameof(ScheduleModel.MaxConsecutiveFull) => nameof(ScheduleMaxConsecutiveFull),
                nameof(ScheduleModel.MaxFullPerMonth) => nameof(ScheduleMaxFullPerMonth),
                nameof(ScheduleModel.ShopId) => nameof(PendingSelectedShop),
                _ => key
            };
        }
        private static bool TryParseShiftInterval(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // приймаємо "08:00 - 16:00" та "08:00-16:00"
            var parts = text.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            return TimeSpan.TryParseExact(parts[0], "hh\\:mm", CultureInfo.InvariantCulture, out _)
                && TimeSpan.TryParseExact(parts[1], "hh\\:mm", CultureInfo.InvariantCulture, out _);
        }

        private void ShowValidationMessageBox()
        {
            var msgs = new List<string>();

            foreach (var err in GetErrors(null))
            {
                if (err is string s && !string.IsNullOrWhiteSpace(s))
                    msgs.Add(s);
                else if (err != null)
                    msgs.Add(err.ToString() ?? string.Empty);
            }

            var text = string.Join(Environment.NewLine, msgs.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

            if (!string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(
                    text,
                    "Перевірте введені дані",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ShowValidationSummary()
        {
            var all = new List<string>();

            foreach (var e in GetErrors(null))
            {
                if (e is string s && !string.IsNullOrWhiteSpace(s))
                    all.Add(s);
                else if (e != null)
                    all.Add(e.ToString() ?? string.Empty);
            }

            var text = string.Join(Environment.NewLine, all.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            if (string.IsNullOrWhiteSpace(text))
                return;

            CustomMessageBox.Show(
                "Validation",
                text,
                CustomMessageBoxIcon.Error,
                okText: "OK");
        }

        private static string BuildValidationSummary(IReadOnlyDictionary<string, string> errors)
        {
            var sb = new StringBuilder();

            foreach (var msg in errors.Values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
                sb.AppendLine(msg);

            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text) ? "Please check the input values." : text;
        }

    }
}

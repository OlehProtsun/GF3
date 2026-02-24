using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using WPFApp.MVVM.Validation.Rules;
using WPFApp.UI.Dialogs;
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
        private bool ValidateBeforeSave(
            bool showDialog = true,
            bool requireShift2 = false,
            bool requireAvailabilityGroup = false)
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

            // 2) ЄДИНЕ джерело базових правил для ScheduleModel
            var raw = ScheduleValidationRules.ValidateAll(model);

            // 3) ЄДИНЕ місце, де ключі приводяться до UI-binding property
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kv in raw)
            {
                var vmKey = MapValidationKeyToVm(kv.Key);
                if (!errors.ContainsKey(vmKey))
                    errors[vmKey] = kv.Value;
            }

            // 4a) Extra requirements (context-dependent)
            // Generate needs AvailabilityGroup + Shift2; Save can be more permissive.
            if (requireAvailabilityGroup)
            {
                if (SelectedAvailabilityGroup is null || SelectedAvailabilityGroup.Id <= 0)
                {
                    errors[nameof(SelectedAvailabilityGroup)] = "Please select an availability group.";
                }
            }

            if (requireShift2)
            {
                // Base rules allow empty Shift2 (optional). For Generate we require it.
                if (string.IsNullOrWhiteSpace(model.Shift2Time))
                {
                    errors[nameof(ScheduleShift2)] = "Shift 2 time is required (example: 09:00 - 18:00).";
                }
            }

            // 5) Period mismatch (якщо в AvailabilityGroup є Year/Month і вони не співпадають)
            if (SelectedAvailabilityGroup is not null
                && TryGetAvailabilityGroupPeriod(SelectedAvailabilityGroup, out var gy, out var gm)
                && model.Year > 0 && model.Month is >= 1 and <= 12
                && (gy != model.Year || gm != model.Month))
            {
                errors[nameof(SelectedAvailabilityGroup)] =
                    $"Selected availability group is for {gy:D4}-{gm:D2}, but schedule is {model.Year:D4}-{model.Month:D2}.";
            }

            // 6) MinHours per employee (для MessageBox). Підсвітка клітинок йде через ValidationRule в XAML
            if (SelectedBlock.Employees
                    .Where(e => e != null && IsAvailabilityEmployee(e.EmployeeId))
                    .Any(e => e.MinHoursMonth < 1))
            {
                errors[nameof(ScheduleEmployees)] = "Min hours per employee must be at least 1.";
            }


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

            // Вирівнюємо модель під фактичний Selected* (навіть якщо debounce ще не встиг)
            var shopId = SelectedShop?.Id ?? 0;
            if (ScheduleShopId != shopId)
                ScheduleShopId = shopId;

            var groupId = SelectedAvailabilityGroup?.Id ?? 0;
            if (SelectedBlock.SelectedAvailabilityGroupId != groupId)
            {
                SelectedBlock.SelectedAvailabilityGroupId = groupId;
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);
            }
        }

        /// <summary>
        /// Обгортка над Save, яка спочатку запускає повну валідацію.
        /// Якщо є помилки — ми НЕ викликаємо _owner.SaveScheduleAsync().
        /// </summary>
        private async Task SaveWithValidationAsync()
        {
            var ok = await Application.Current.Dispatcher
                .InvokeAsync(() => ValidateBeforeSave(showDialog: true));

            if (!ok)
                return;

            await _owner.SaveScheduleAsync().ConfigureAwait(false);
        }               /// Обгортка над Generate, яка спочатку запускає повну валідацію.
                        /// Якщо є помилки — генерацію не запускаємо.
                        /// </summary>
        private async Task GenerateWithValidationAsync()
        {
            var ok = await Application.Current.Dispatcher
                .InvokeAsync(() => ValidateBeforeSave(
                    showDialog: true,
                    requireShift2: true,
                    requireAvailabilityGroup: true));


            if (!ok)
                return;

            await _owner.GenerateScheduleAsync().ConfigureAwait(false);

            await Application.Current.Dispatcher
                .InvokeAsync(async () => await RefreshScheduleMatrixAsync());
        }


        private static string MapValidationKeyToVm(string key)
        {
            return key switch
            {
                // ScheduleValidationRules.K_ScheduleShopId == "ScheduleShopId"
                ScheduleValidationRules.K_ScheduleShopId => nameof(SelectedShop),

                _ => key
            };
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

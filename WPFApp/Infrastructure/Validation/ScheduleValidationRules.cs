using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using WPFApp.Infrastructure.ScheduleMatrix;

namespace WPFApp.Infrastructure.Validation
{
    /// <summary>
    /// ScheduleValidationRules — “єдине місце”, де живуть правила валідації
    /// для форми редагування/створення Schedule.
    ///
    /// НАВІЩО окремий файл:
    /// 1) ViewModel не роздувається правилами (“що можна/не можна”).
    /// 2) Правила можна повторно використовувати (наприклад у сервісі/owner/іншій формі).
    /// 3) Легко тестувати (навіть без WPF).
    ///
    /// ЯК ми повертаємо помилки:
    /// - ключ: назва властивості з ViewModel (наприклад "ScheduleName")
    /// - значення: текст помилки
    ///
    /// Чому ключ саме VM-властивість:
    /// - INotifyDataErrorInfo у WPF показує помилку біля того поля,
    ///   binding якого має таку ж назву propertyName.
    /// </summary>
    public static class ScheduleValidationRules
    {
        // ------------------------------------------------------------
        // 1) Ключі (імена властивостей) — як у твоєму ViewModel
        // ------------------------------------------------------------
        //
        // Важливо: ці рядки мають відповідати іменам публічних властивостей у VM.
        // Наприклад у VM є:
        //    public string ScheduleName { get; set; }
        // значить ключ має бути "ScheduleName".
        //
        // Так WPF зрозуміє: “помилка належить саме цьому полю”.
        //
        public const string K_ScheduleShopId = "ScheduleShopId";
        public const string K_ScheduleName = "ScheduleName";
        public const string K_ScheduleYear = "ScheduleYear";
        public const string K_ScheduleMonth = "ScheduleMonth";
        public const string K_SchedulePeoplePerShift = "SchedulePeoplePerShift";
        public const string K_ScheduleShift1 = "ScheduleShift1";
        public const string K_ScheduleShift2 = "ScheduleShift2";
        public const string K_ScheduleMaxHoursPerEmp = "ScheduleMaxHoursPerEmp";
        public const string K_ScheduleMaxConsecutiveDays = "ScheduleMaxConsecutiveDays";
        public const string K_ScheduleMaxConsecutiveFull = "ScheduleMaxConsecutiveFull";
        public const string K_ScheduleMaxFullPerMonth = "ScheduleMaxFullPerMonth";
        public const string K_ScheduleNote = "ScheduleNote";

        // ------------------------------------------------------------
        // 2) Публічний метод: валідувати ВСЮ модель і повернути всі помилки
        // ------------------------------------------------------------
        /// <summary>
        /// Валідує весь ScheduleModel і повертає словник помилок:
        /// key = назва VM-властивості, value = текст помилки.
        ///
        /// Якщо помилок немає — повертається порожній словник.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ValidateAll(ScheduleModel? model)
        {
            var errors = new Dictionary<string, string>(StringComparer.Ordinal);

            // Якщо моделі нема — нічого валідити (або можна повернути загальну помилку)
            if (model is null)
                return errors;

            // 1) ShopId
            AddIfError(errors, K_ScheduleShopId, ValidateShopId(model.ShopId));

            // 2) Name
            AddIfError(errors, K_ScheduleName, ValidateName(model.Name));

            // 3) Year / Month
            AddIfError(errors, K_ScheduleYear, ValidateYear(model.Year));
            AddIfError(errors, K_ScheduleMonth, ValidateMonth(model.Month));

            // 4) PeoplePerShift
            AddIfError(errors, K_SchedulePeoplePerShift, ValidatePeoplePerShift(model.PeoplePerShift));

            // 5) Shift1 (обов’язковий)
            var shift1Err = ValidateShift(model.Shift1Time, required: true, out var s1From, out var s1To);
            AddIfError(errors, K_ScheduleShift1, shift1Err);

            // 6) Shift2 (не обов’язковий, але якщо заданий — має бути валідний)
            var shift2Err = ValidateShift(model.Shift2Time, required: false, out var s2From, out var s2To);
            AddIfError(errors, K_ScheduleShift2, shift2Err);

            // 7) Якщо обидва shifts валідні — можна перевірити, що вони не перетинаються
            // (це не “must”, але часто логічно)
            if (shift1Err is null && shift2Err is null && s1From.HasValue && s1To.HasValue && s2From.HasValue && s2To.HasValue)
            {
                if (IntervalsOverlap(s1From.Value, s1To.Value, s2From.Value, s2To.Value))
                {
                    // Логічніше вішати на Shift2, бо зазвичай Shift1 базовий
                    AddIfError(errors, K_ScheduleShift2, "Shift2 overlaps Shift1.");
                }
            }

            // 8) Ліміти (всі мають бути >= 0)
            AddIfError(errors, K_ScheduleMaxHoursPerEmp, ValidateNonNegative(model.MaxHoursPerEmpMonth, "Max hours per employee must be >= 0."));
            AddIfError(errors, K_ScheduleMaxConsecutiveDays, ValidateNonNegative(model.MaxConsecutiveDays, "Max consecutive days must be >= 0."));
            AddIfError(errors, K_ScheduleMaxConsecutiveFull, ValidateNonNegative(model.MaxConsecutiveFull, "Max consecutive full must be >= 0."));
            AddIfError(errors, K_ScheduleMaxFullPerMonth, ValidateNonNegative(model.MaxFullPerMonth, "Max full per month must be >= 0."));

            // 9) Перехресні правила між лімітами (опціонально, але логічно)
            if (model.MaxConsecutiveFull > model.MaxConsecutiveDays)
                AddIfError(errors, K_ScheduleMaxConsecutiveFull, "Max consecutive full cannot be greater than max consecutive days.");

            // 10) Note — обмеження довжини (опціонально)
            AddIfError(errors, K_ScheduleNote, ValidateNote(model.Note));

            return errors;
        }

        // ------------------------------------------------------------
        // 3) Публічний метод: валідувати ОДНЕ поле (для inline-валідації)
        // ------------------------------------------------------------
        /// <summary>
        /// Повертає повідомлення помилки для конкретної властивості ViewModel.
        ///
        /// Навіщо:
        /// - щоб у VM можна було валідити тільки змінене поле,
        ///   замість того щоб кожен раз робити ValidateAll().
        ///
        /// Якщо поле валідне або невідоме — повертає null.
        /// </summary>
        public static string? ValidateProperty(ScheduleModel? model, string vmPropertyName)
        {
            if (model is null || string.IsNullOrWhiteSpace(vmPropertyName))
                return null;

            // switch по імені VM-властивості
            return vmPropertyName switch
            {
                K_ScheduleShopId => ValidateShopId(model.ShopId),
                K_ScheduleName => ValidateName(model.Name),
                K_ScheduleYear => ValidateYear(model.Year),
                K_ScheduleMonth => ValidateMonth(model.Month),
                K_SchedulePeoplePerShift => ValidatePeoplePerShift(model.PeoplePerShift),
                K_ScheduleShift1 => ValidateShift(model.Shift1Time, required: true, out _, out _),
                K_ScheduleShift2 => ValidateShift(model.Shift2Time, required: false, out _, out _),
                K_ScheduleMaxHoursPerEmp => ValidateNonNegative(model.MaxHoursPerEmpMonth, "Max hours per employee must be >= 0."),
                K_ScheduleMaxConsecutiveDays => ValidateNonNegative(model.MaxConsecutiveDays, "Max consecutive days must be >= 0."),
                K_ScheduleMaxConsecutiveFull => ValidateNonNegative(model.MaxConsecutiveFull, "Max consecutive full must be >= 0."),
                K_ScheduleMaxFullPerMonth => ValidateNonNegative(model.MaxFullPerMonth, "Max full per month must be >= 0."),
                K_ScheduleNote => ValidateNote(model.Note),
                _ => null
            };
        }

        // ------------------------------------------------------------
        // 4) Нижче — приватні “цеглинки” (конкретні правила)
        // ------------------------------------------------------------

        private static string? ValidateShopId(int shopId)
        {
            // У більшості форм ShopId має бути вибраний (> 0)
            if (shopId <= 0)
                return "Please select a shop.";

            return null;
        }

        private static string? ValidateName(string? name)
        {
            name = (name ?? string.Empty).Trim();

            if (name.Length == 0)
                return "Name is required.";

            if (name.Length > 100)
                return "Name is too long (max 100 chars).";

            return null;
        }

        private static string? ValidateYear(int year)
        {
            // Тут можна налаштувати діапазон під свій продукт.
            // Я роблю “безпечний” загальний діапазон.
            if (year < 2000 || year > 2100)
                return "Year must be between 2000 and 2100.";

            return null;
        }

        private static string? ValidateMonth(int month)
        {
            if (month < 1 || month > 12)
                return "Month must be between 1 and 12.";

            return null;
        }

        private static string? ValidatePeoplePerShift(int peoplePerShift)
        {
            if (peoplePerShift <= 0)
                return "People per shift must be > 0.";

            if (peoplePerShift > 200)
                return "People per shift is too large.";

            return null;
        }

        /// <summary>
        /// Валідація shift-рядка.
        ///
        /// expected:
        ///  - "HH:mm - HH:mm"
        ///  - або "HH:mm-HH:mm"
        ///
        /// required=true:
        ///  - пустий рядок => помилка
        /// required=false:
        ///  - пустий рядок => ОК (друга зміна може бути необов'язковою)
        ///
        /// out from/to:
        ///  - якщо shift валідний — повертаємо розпарсені TimeSpan
        ///  - якщо невалідний — out буде null
        /// </summary>
        private static string? ValidateShift(string? shiftText, bool required, out TimeSpan? from, out TimeSpan? to)
        {
            from = null;
            to = null;

            shiftText = (shiftText ?? string.Empty).Trim();

            if (shiftText.Length == 0)
                return required ? "Shift time is required (example: 09:00 - 18:00)." : null;

            // Розділяємо по '-' (перший раз). Дозволяємо пробіли.
            var parts = shiftText.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return "Shift format must be: HH:mm - HH:mm.";

            // Парсимо часи (використовуємо той самий парсер, що і матриця)
            if (!ScheduleMatrixEngine.TryParseTime(parts[0], out var f) ||
                !ScheduleMatrixEngine.TryParseTime(parts[1], out var t))
            {
                return "Shift time must be HH:mm (example: 09:00 - 18:00).";
            }

            // Логіка: старт має бути < кінець (без “перехід через північ” у shift)
            // Якщо тобі потрібні нічні зміни — скажеш, і я адаптую правило.
            if (t <= f)
                return "Shift end must be later than shift start.";

            // Додатковий sanity-check: щоб shift не був 0 або надто довгим
            var dur = t - f;
            if (dur.TotalMinutes < 30)
                return "Shift duration is too short.";

            if (dur.TotalHours > 24)
                return "Shift duration is too long.";

            from = f;
            to = t;
            return null;
        }

        private static string? ValidateNonNegative(int value, string messageIfInvalid)
        {
            if (value < 0)
                return messageIfInvalid;

            return null;
        }

        private static string? ValidateNote(string? note)
        {
            note = note ?? string.Empty;

            if (note.Length > 1000)
                return "Note is too long (max 1000 chars).";

            return null;
        }

        /// <summary>
        /// Перевірка перетину інтервалів [aFrom, aTo) і [bFrom, bTo).
        /// Для shift-ів (звичайно в межах доби).
        /// </summary>
        private static bool IntervalsOverlap(TimeSpan aFrom, TimeSpan aTo, TimeSpan bFrom, TimeSpan bTo)
        {
            // Перетин існує, якщо старт одного менший за кінець іншого і навпаки.
            return aFrom < bTo && bFrom < aTo;
        }

        /// <summary>
        /// Додає помилку в словник, якщо errorMessage не null/empty.
        /// Якщо для цього ключа вже є помилка — НЕ перезаписуємо (перший текст лишаємо),
        /// щоб UI не “миготів” різними повідомленнями.
        /// </summary>
        private static void AddIfError(Dictionary<string, string> errors, string key, string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return;

            if (!errors.ContainsKey(key))
                errors[key] = errorMessage;
        }
    }
}

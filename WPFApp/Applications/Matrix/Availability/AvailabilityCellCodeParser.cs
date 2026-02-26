using System;
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Enums;

namespace WPFApp.Applications.Matrix.Availability
{
    /// <summary>
    /// Адаптер для UI, який нормалізує/перевіряє “коди доступності” в клітинках матриці.
    ///
    /// Навіщо існує:
    /// - У UI користувач вводить короткі маркери (наприклад "+", "-") або інтервал часу ("09:00-18:00").
    /// - У BLL (Business Logic Layer) є "канонічний" парсер <see cref="AvailabilityCodeParser"/>,
    ///   який знає всі допустимі формати та семантику.
    /// - Цей клас виступає "UI adapter": не дублює повний парсинг, а делегує його BLL,
    ///   після чого застосовує UI-специфічні правила:
    ///   * trim/порожній ввід = ок
    ///   * нормалізація інтервалу до стандартного вигляду ("09:00 - 18:00" -> "09:00-18:00")
    ///   * додаткова валідація "кінець > початок" (якщо allowOvernight == false)
    ///
    /// Повертає:
    /// - bool: успіх нормалізації/валідації
    /// - normalized: рядок, який потрібно записати назад в клітинку (стандартизований вигляд)
    /// - error: текст помилки (якщо повернули false), призначений для показу в UI
    ///
    /// NOTE:
    /// - Порожній рядок вважається валідним (означає “немає значення/не задано”).
    /// - allowOvernight: якщо true, то допускаються інтервали що перетинають північ (наприклад "22:00-06:00").
    ///   Якщо false, то такі інтервали забороняються (to <= from => error).
    /// </summary>
    public static class AvailabilityCellCodeParser
    {
        /// <summary>
        /// Маркер "доступний будь-коли" (символ/рядок визначений у BLL).
        /// Виноситься назовні, щоб UI не хардкодив цей символ.
        /// </summary>
        public static string AnyMark => AvailabilityCodeParser.AnyMark;

        /// <summary>
        /// Маркер "недоступний" (символ/рядок визначений у BLL).
        /// Виноситься назовні, щоб UI не хардкодив цей символ.
        /// </summary>
        public static string NoneMark => AvailabilityCodeParser.NoneMark;

        /// <summary>
        /// Нормалізує введений користувачем текст (raw) у "канонічний" вигляд та перевіряє коректність.
        ///
        /// Вхід:
        /// - raw: те, що користувач ввів у клітинку (може бути null/порожнє/з пробілами)
        /// - allowOvernight:
        ///   * false (default): забороняє інтервали, де кінець <= початку (наприклад "18:00-09:00")
        ///   * true: дозволяє “overnight” інтервали (наприклад "22:00-06:00")
        ///
        /// Вихід:
        /// - normalized:
        ///   * якщо інтервал: "HH:mm-HH:mm" без зайвих пробілів (навіть якщо ввели "HH:mm - HH:mm")
        ///   * якщо ANY/NONE: відповідний маркер AnyMark/NoneMark
        ///   * якщо порожньо: "" (залишається пустим)
        /// - error:
        ///   * null при успіху
        ///   * дружній текст для UI при помилці формату/валідації
        ///
        /// Повертає:
        /// - true: якщо raw валідний або порожній
        /// - false: якщо raw не розпарсився або порушив додаткові правила (наприклад, to <= from)
        /// </summary>
        public static bool TryNormalize(string? raw, out string normalized, out string? error, bool allowOvernight = false)
        {
            // Ініціалізація out-параметрів дефолтами (важливо: навіть при ранньому return вони будуть встановлені).
            normalized = string.Empty;
            error = null;

            // 1) Приводимо ввід до передбачуваного вигляду:
            // - null -> ""
            // - Trim() прибирає пробіли з країв (типовий шум при вводі/копіюванні).
            raw = (raw ?? string.Empty).Trim();

            // 2) Порожній ввід — валідний.
            // Це означає: користувач нічого не задав для цього дня (не помилка).
            if (raw.Length == 0)
                return true;

            // 3) Делегуємо первинний парсинг у BLL:
            // AvailabilityCodeParser.TryParse розбирає:
            // - маркери "+"/"-" (або інші, якщо BLL так визначає)
            // - інтервал часу "HH:mm-HH:mm" (можливо також "HH:mm - HH:mm")
            // Повертає:
            // - parsedKind: AvailabilityKind (ANY/NONE/INT/...)
            // - interval: строкове представлення інтервалу (для INT)
            if (!AvailabilityCodeParser.TryParse(raw, out var parsedKind, out var interval))
            {
                // Якщо BLL не розпізнав формат — повертаємо пояснення для UI.
                // Тут "Allowed:" — це UI-орієнтований гайд, що саме можна вводити.
                error = "Allowed: +, -, HH:mm-HH:mm or HH:mm - HH:mm (e.g., 09:00-18:00).";
                return false;
            }

            // 4) Якщо це інтервал (AvailabilityKind.INT) і interval не порожній —
            // застосовуємо додаткову перевірку (за потреби) + нормалізацію.
            if (parsedKind == AvailabilityKind.INT && !string.IsNullOrWhiteSpace(interval))
            {
                // Додаткова бізнес-умова на UI-рівні:
                // якщо allowOvernight == false, то забороняємо інтервал, де кінець <= початку.
                // Це часто означає:
                // - або помилку користувача
                // - або “overnight” інтервал, який у цьому режимі не підтримується
                if (!allowOvernight)
                {
                    // Розбиваємо "from-to" рівно на 2 частини.
                    // TrimEntries прибирає зайві пробіли навколо "from" і "to".
                    var parts = interval.Split('-', 2, StringSplitOptions.TrimEntries);

                    // Перевіряємо:
                    // - є дві частини
                    // - обидві частини коректно парсяться у час (ScheduleMatrixEngine.TryParseTime)
                    // - to <= from => помилка (в межах одного дня кінець має бути пізніше старту)
                    if (parts.Length == 2
                        && BusinessLogicLayer.Schedule.ScheduleMatrixEngine.TryParseTime(parts[0], out var from)
                        && BusinessLogicLayer.Schedule.ScheduleMatrixEngine.TryParseTime(parts[1], out var to)
                        && to <= from)
                    {
                        error = "End time must be later than start time.";
                        return false;
                    }
                }

                // Нормалізація інтервалу: прибираємо " - " і робимо канонічний "HH:mm-HH:mm".
                // Це зручно для:
                // - однакового відображення в UI
                // - стабільних порівнянь/збереження/експорту
                normalized = interval.Replace(" - ", "-");
                return true;
            }

            // 5) Якщо це НЕ інтервал — тоді це маркерний тип:
            // ANY => AnyMark
            // NONE => NoneMark
            // інше => "" (fallback; теоретично BLL може повернути ще якісь kind, але UI їх не підтримує)
            normalized = parsedKind switch
            {
                AvailabilityKind.ANY => AnyMark,
                AvailabilityKind.NONE => NoneMark,
                _ => string.Empty
            };

            return true;
        }
    }
}
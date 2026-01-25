using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFApp.Infrastructure.Validation;

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
        private bool ValidateBeforeSave()
        {
            // 0) Якщо немає активного блоку або моделі — немає що валідити.
            // Це “не помилка валідації”, просто стан такий (наприклад форма порожня).
            // Тут можна було б повернути false, якщо Save без SelectedBlock неможливий,
            // але у твоєму UI зазвичай кнопки і так будуть заблоковані/неактивні.
            if (SelectedBlock?.Model is not { } model)
                return true;

            // 1) Запускаємо повну валідацію ВСІХ полів моделі.
            // Повертається словник: { "ScheduleName" -> "Name is required", ... }.
            //
            // Ключі тут повинні збігатися з іменами властивостей у ViewModel,
            // тоді WPF правильно “прикріпить” помилки до потрібних контролів.
            var errors = ScheduleValidationRules.ValidateAll(model);

            // 2) Перезаписуємо помилки у нашому сховищі ValidationErrors:
            // - якщо помилок немає => SetValidationErrors очистить все
            // - якщо помилки є => вони підуть у _validation і піднімуть ErrorsChanged
            //
            // Це важливо: UI одразу підсвітить всі проблемні поля.
            SetValidationErrors(errors);

            // 3) Якщо HasErrors == true, значить є хоча б одна помилка,
            // і далі продовжувати Save/Generate не можна.
            return !HasErrors;
        }

        /// <summary>
        /// Обгортка над Save, яка спочатку запускає повну валідацію.
        /// Якщо є помилки — ми НЕ викликаємо _owner.SaveScheduleAsync().
        /// </summary>
        private Task SaveWithValidationAsync()
        {
            // 1) Перевіряємо всі правила
            if (!ValidateBeforeSave())
                return Task.CompletedTask;

            // 2) Якщо помилок немає — виконуємо реальне збереження
            return _owner.SaveScheduleAsync();
        }

        /// <summary>
        /// Обгортка над Generate, яка спочатку запускає повну валідацію.
        /// Якщо є помилки — генерацію не запускаємо.
        /// </summary>
        private Task GenerateWithValidationAsync()
        {
            if (!ValidateBeforeSave())
                return Task.CompletedTask;

            return _owner.GenerateScheduleAsync();
        }


    }
}

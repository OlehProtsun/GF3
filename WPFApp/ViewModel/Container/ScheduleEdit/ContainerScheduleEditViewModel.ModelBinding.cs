using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFApp.Infrastructure.Validation;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    /// <summary>
    /// ModelBinding.cs — частина ViewModel, яка відповідає за “універсальний setter” полів ScheduleModel.
    ///
    /// У тебе багато властивостей вигляду:
    ///   public int ScheduleYear { get => ...; set => SetScheduleValue(...); }
    ///
    /// Щоб не дублювати однакову логіку:
    /// - перевірити, чи є SelectedBlock/Model
    /// - порівняти old/new
    /// - записати в модель
    /// - підняти OnPropertyChanged
    /// - почистити помилку валідації для цього поля
    /// - опційно інваліднути згенерований розклад
    /// - запустити inline-валидацію через ScheduleValidationRules.ValidateProperty
    ///
    /// ми тримаємо 1 метод SetScheduleValue<T>.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        /// <summary>
        /// Універсальний setter для значень у ScheduleModel.
        ///
        /// Параметри:
        /// - value: нове значення, яке прийшло з UI
        /// - get: як отримати поточне значення з моделі (для порівняння)
        /// - set: як записати нове значення в модель
        ///
        /// - clearErrors:
        ///   якщо true — перед новою перевіркою прибираємо стару помилку для цього поля
        ///
        /// - invalidateGenerated:
        ///   якщо true — це поле впливає на “структуру” розкладу,
        ///   і після його зміни треба скинути згенеровані слоти/матриці
        ///   (наприклад Year або Month)
        ///
        /// - propertyName:
        ///   автоматично підтягується як ім’я властивості, яка викликала SetScheduleValue
        ///   (ScheduleYear, ScheduleMonth, ...), якщо не передавати вручну.
        ///
        /// Повертає:
        /// - true: значення реально змінилося (ми записали в модель)
        /// - false: або немає model, або значення не змінилось (нічого не робили)
        /// </summary>
        private bool SetScheduleValue<T>(
            T value,
            Func<ScheduleModel, T> get,
            Action<ScheduleModel, T> set,
            bool clearErrors = true,
            bool invalidateGenerated = false,
            [CallerMemberName] string? propertyName = null)
        {
            // 1) Без SelectedBlock/Model нічого міняти
            if (SelectedBlock?.Model is not { } model)
                return false;

            // 2) Якщо значення не змінилось — нічого не робимо
            var current = get(model);
            if (EqualityComparer<T>.Default.Equals(current, value))
                return false;

            // 3) Записуємо в модель
            set(model, value);

            // 4) Повідомляємо UI, що властивість змінилась
            if (propertyName != null)
                OnPropertyChanged(propertyName);

            // 5) Прибираємо старі помилки (якщо обрано clearErrors)
            //    Це важливо: старе повідомлення не повинно висіти, якщо користувач змінив значення.
            if (clearErrors && propertyName != null)
                ClearValidationErrors(propertyName);

            // 6) Якщо поле впливає на генерацію — скидаємо згенерований розклад
            if (invalidateGenerated)
                InvalidateGeneratedSchedule(clearPreviewMatrix: true);

            // 7) Inline-валидація тільки цього поля:
            //    - повертає або null (ок), або текст помилки
            if (propertyName != null)
            {
                var msg = ScheduleValidationRules.ValidateProperty(model, propertyName);
                if (!string.IsNullOrWhiteSpace(msg))
                    AddValidationError(propertyName, msg);
            }

            return true;
        }
    }
}

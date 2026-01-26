using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WPFApp.Infrastructure
{
    /// <summary>
    /// ViewModelBase — канонічна базова VM для WPF:
    /// - реалізує INotifyPropertyChanged
    /// - надає SetProperty(...) з EqualityComparer<T>.Default
    /// - має OnPropertiesChanged(...) для computed properties
    ///
    /// Оптимізації:
    /// 1) Кеш PropertyChangedEventArgs по назві властивості (менше алокацій).
    /// 2) Безпечний raise з не-UI потоку: маршалимо на Dispatcher, якщо потрібно.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        // Подія зміни властивостей.
        public event PropertyChangedEventHandler? PropertyChanged;

        // Кеш аргументів PropertyChangedEventArgs по назві властивості.
        // Це зменшує кількість new PropertyChangedEventArgs(...) при частих оновленнях.
        private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _argsCache =
            new(StringComparer.Ordinal);

        /// <summary>
        /// OnPropertyChanged — підняти PropertyChanged для propertyName.
        /// Якщо виклик з background thread — підніме на UI thread (Dispatcher).
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Забираємо handler у локальну змінну, щоб уникнути race condition на відписках.
            var handler = PropertyChanged;

            // Якщо ніхто не підписаний — робити нічого.
            if (handler is null)
                return;

            // Отримуємо PropertyChangedEventArgs:
            // - якщо propertyName null/empty — створюємо новий (такий кейс рідкісний)
            // - якщо ім'я є — беремо з кешу
            var args = GetArgs(propertyName);

            // Якщо є Application.Current і Dispatcher — перевіряємо доступ до UI потоку.
            var dispatcher = Application.Current?.Dispatcher;

            // Якщо dispatcher існує і ми НЕ в UI потоці — маршалимо.
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                // BeginInvoke: не блокуємо фоновий потік.
                dispatcher.BeginInvoke(new Action(() => handler(this, args)));
                return;
            }

            // Якщо ми вже на UI потоці (або dispatcher нема) — піднімаємо одразу.
            handler(this, args);
        }

        /// <summary>
        /// SetProperty — встановити значення в backing field і підняти PropertyChanged
        /// ТІЛЬКИ якщо значення реально змінилось.
        /// </summary>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            // EqualityComparer<T>.Default:
            // - коректно для struct
            // - коректно для override Equals
            // - швидше/правильніше ніж object.Equals для generic
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            // Записуємо нове значення у поле.
            field = value;

            // Нотифікуємо UI.
            OnPropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Перевантаження SetProperty з afterChange callback.
        /// Корисно, коли треба:
        /// - змінити інші computed properties
        /// - оновити команди
        /// але лише якщо значення реально змінилось.
        /// </summary>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            Action afterChange,
            [CallerMemberName] string? propertyName = null)
        {
            // Якщо не змінилось — нічого не робимо.
            if (!SetProperty(ref field, value, propertyName))
                return false;

            // Якщо змінилось — викликаємо afterChange.
            afterChange?.Invoke();

            return true;
        }

        /// <summary>
        /// OnPropertiesChanged — підняти PropertyChanged для кількох властивостей.
        /// Корисно для computed properties: A змінилось => OnPropertiesChanged(nameof(X), nameof(Y)).
        /// </summary>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            // Null-safe.
            if (propertyNames is null || propertyNames.Length == 0)
                return;

            // Піднімаємо по черзі.
            for (int i = 0; i < propertyNames.Length; i++)
                OnPropertyChanged(propertyNames[i]);
        }

        private static PropertyChangedEventArgs GetArgs(string? propertyName)
        {
            // Якщо ім'я відсутнє — це означає "оновилось все".
            // Такий кейс рідкісний, тому без кешу.
            if (string.IsNullOrEmpty(propertyName))
                return new PropertyChangedEventArgs(propertyName);

            // Кешуємо args по propertyName.
            return _argsCache.GetOrAdd(propertyName, static n => new PropertyChangedEventArgs(n));
        }
    }
}

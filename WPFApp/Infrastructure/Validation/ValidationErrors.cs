using System;                         // Базові типи .NET (EventHandler, Array, тощо)
using System.Collections;             // IEnumerable (негернерік) потрібен для INotifyDataErrorInfo.GetErrors
using System.Collections.Generic;     // Dictionary, List
using System.ComponentModel;          // INotifyDataErrorInfo, DataErrorsChangedEventArgs
using System.Linq;                    // SelectMany, ToList

namespace WPFApp.Infrastructure.Validation
{
    // sealed = від цього класу НЕ можна успадковуватись.
    // Тут це логічно: це маленький службовий клас, його не планують розширювати через спадкування.
    public sealed class ValidationErrors : INotifyDataErrorInfo
    {
        // _errors — "сховище" помилок валідації.
        // Ключ (string) = назва властивості (наприклад "Email", "Password").
        // Значення (List<string>) = список текстів помилок для цієї властивості.
        // Приклад:
        //   _errors["Email"] = ["Email обов'язковий", "Невірний формат email"]
        private readonly Dictionary<string, List<string>> _errors = new();

        // Подія, яку слухає WPF (через binding), щоб дізнатись:
        // "Для цієї властивості змінився список помилок — онови UI".
        // ? означає, що подія може бути null, якщо її ніхто не підписав.
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        // HasErrors — швидка відповідь: є хоч одна помилка чи ні.
        // Якщо словник не порожній — значить хоча б для однієї властивості є помилки.
        public bool HasErrors => _errors.Count > 0;

        // Метод з інтерфейсу INotifyDataErrorInfo.
        // WPF викликає його, коли хоче отримати помилки:
        // - або для конкретної властивості (propertyName = "Email")
        // - або для всього об'єкта загалом (propertyName = null або порожній)
        public IEnumerable GetErrors(string? propertyName)
        {
            // Якщо назву властивості не передали (null/""/"   "),
            // повертаємо всі помилки по всіх властивостях одразу.
            if (string.IsNullOrWhiteSpace(propertyName))
                return _errors.SelectMany(x => x.Value); // "склеюємо" всі списки помилок в один

            // Якщо propertyName заданий — намагаємось знайти список помилок для цієї властивості.
            // TryGetValue:
            //   - повертає true, якщо ключ знайдений
            //   - в out var list кладе знайдений список
            // Якщо ключа нема — повертаємо порожній масив рядків (без помилок).
            return _errors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();
        }

        // ClearAll — повністю очищає всі помилки.
        // Важливий момент: після очищення треба "повідомити" UI,
        // що для кожної властивості помилки змінились.
        public void ClearAll()
        {
            // Зберігаємо список ключів ДО очищення.
            // Бо після _errors.Clear() ключів уже не буде.
            var keys = _errors.Keys.ToList();

            // Очищаємо весь словник (всі помилки для всіх властивостей).
            _errors.Clear();

            // Для кожної властивості, яка мала помилки, піднімаємо подію,
            // щоб WPF оновив відображення помилок.
            foreach (var k in keys)
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(k));
        }

        // Clear — очищає помилки тільки для однієї властивості (propertyName).
        public void Clear(string propertyName)
        {
            // Remove повертає true, якщо ключ реально існував і був видалений.
            // Якщо помилок для цієї властивості не було — нічого не робимо.
            if (_errors.Remove(propertyName))
                // Повідомляємо UI: "для цієї властивості список помилок змінився"
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        // Add — додає одну помилку (message) для конкретної властивості (propertyName).
        public void Add(string propertyName, string message)
        {
            // Пробуємо отримати існуючий список помилок для цієї властивості.
            // Якщо його ще нема — створюємо новий список і кладемо в словник.
            if (!_errors.TryGetValue(propertyName, out var list))
                _errors[propertyName] = list = new List<string>();

            // Захист від дублювання:
            // Якщо такий самий текст помилки вже є — вдруге не додаємо.
            if (!list.Contains(message))
            {
                // Додаємо помилку в список.
                list.Add(message);

                // Повідомляємо WPF, що помилки для propertyName змінились,
                // щоб UI (наприклад червона рамка і текст під полем) оновились.
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        // SetMany — зручно задати багато помилок "пакетом".
        // Наприклад, коли сервер повернув помилки по багатьох полях.
        // IReadOnlyDictionary<string, string>:
        //   ключ = назва властивості
        //   значення = текст помилки (одна на поле)
        public void SetMany(IReadOnlyDictionary<string, string> errors)
        {
            // Спочатку прибираємо всі старі помилки,
            // щоб новий набір не змішався зі старим.
            ClearAll();

            // Проходимось по всіх помилках у словнику errors:
            // kv.Key — назва властивості
            // kv.Value — текст помилки
            foreach (var kv in errors)
                // Додаємо кожну помилку стандартним способом,
                // щоб коректно піднімались події ErrorsChanged.
                Add(kv.Key, kv.Value);
        }
    }
}

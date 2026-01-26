using System;
using DataAccessLayer.Models;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Availability.Helpers
{
    /// <summary>
    /// BindRow — UI-обгортка над BindModel, яка:
    /// - підтримує INotifyPropertyChanged (через ObservableObject),
    /// - зручна для редагування в DataGrid (Key/Value/IsActive),
    /// - вміє конвертуватись у доменну модель BindModel і назад.
    ///
    /// Навіщо окремий клас, а не BindModel напряму:
    /// - BindModel зазвичай є “data” класом без PropertyChanged
    /// - UI редагування потребує нотифікацій про зміни властивостей
    /// - у VM зручно тримати саме редагований стан, а не доменний DTO
    /// </summary>
    public sealed class BindRow : ObservableObject
    {
        // backing field для Id (звичайне число, 0 означає “ще не збережено”)
        private int _id;

        /// <summary>
        /// Id — первинний ключ bind-а в БД/сервісі.
        /// </summary>
        public int Id
        {
            get => _id;
            set
            {
                // SetProperty:
                // - перевіряє, чи значення змінилось
                // - присвоює backing field
                // - піднімає PropertyChanged(nameof(Id))
                SetProperty(ref _id, value);
            }
        }

        // backing field для Key.
        // Тримаємо invariant: _key ніколи не null (тільки "" або непорожній текст).
        private string _key = string.Empty;

        /// <summary>
        /// Key — “гаряча клавіша” або ключ bind-а.
        ///
        /// Invariant:
        /// - ніколи не null (в сеттері нормалізуємо null -> "")
        /// Це прибирає необхідність робити Key ?? string.Empty по всьому коду.
        /// </summary>
        public string Key
        {
            get => _key;
            set
            {
                // Нормалізуємо null у "" ще на вході.
                // Це робить клас більш “строгим” і безпечним.
                var safe = value ?? string.Empty;

                // Оновлюємо з PropertyChanged лише якщо реально змінилось.
                SetProperty(ref _key, safe);
            }
        }

        // backing field для Value.
        // Теж тримаємо invariant: _value ніколи не null.
        private string _value = string.Empty;

        /// <summary>
        /// Value — значення bind-а (те, що вставляється/застосовується).
        ///
        /// Invariant:
        /// - ніколи не null (в сеттері нормалізуємо null -> "")
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                // Нормалізація null -> "" для стабільності.
                var safe = value ?? string.Empty;

                // SetProperty сам вирішить, чи піднімати PropertyChanged.
                SetProperty(ref _value, safe);
            }
        }

        // backing field для IsActive.
        private bool _isActive = true;

        /// <summary>
        /// IsActive — чи активний bind.
        /// Якщо false — він не повинен застосовуватись під час пошуку по hotkey.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                // Стандартне оновлення з PropertyChanged.
                SetProperty(ref _isActive, value);
            }
        }

        /// <summary>
        /// IsBlank — допоміжна властивість для логіки UI:
        /// “рядок фактично порожній” (можна не зберігати / не апсертити).
        ///
        /// Її зручно використовувати в owner’і, щоб уникнути дублювання перевірок.
        /// </summary>
        public bool IsBlank
        {
            get
            {
                // Якщо Key і Value обидва порожні/пробільні — рядок “порожній”.
                // Тут ми використовуємо IsNullOrWhiteSpace, бо користувач може ввести пробіли.
                return string.IsNullOrWhiteSpace(Key) && string.IsNullOrWhiteSpace(Value);
            }
        }

        /// <summary>
        /// ToModel — перетворення UI-рядка в доменну модель для Create/Update.
        ///
        /// Завдяки invariant’ам Key/Value не null — можемо віддавати напряму.
        /// </summary>
        public BindModel ToModel()
        {
            // Створюємо новий BindModel, який піде в сервіс/репозиторій.
            return new BindModel
            {
                // Id (0 => create, >0 => update)
                Id = Id,

                // Key/Value гарантовано не null.
                Key = Key,
                Value = Value,

                // Прапорець активності.
                IsActive = IsActive
            };
        }

        /// <summary>
        /// UpdateFromModel — “накласти” дані BindModel на існуючий BindRow.
        ///
        /// Навіщо:
        /// - якщо ти захочеш робити reload bind-ів без повного пересоздання колекції,
        ///   можна буде оновлювати існуючі BindRow і не губити Selection/Focus у DataGrid.
        /// </summary>
        public void UpdateFromModel(BindModel model)
        {
            // Захист від null — краще отримати зрозумілу помилку на ранньому етапі.
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            // Присвоюємо через властивості (а не поля), щоб:
            // - SetProperty коректно піднімав PropertyChanged
            // - UI оновився
            Id = model.Id;

            // model.Key може бути null у DTO — нормалізуємо в "".
            Key = model.Key ?? string.Empty;

            // model.Value може бути null — теж нормалізуємо.
            Value = model.Value ?? string.Empty;

            // IsActive як є.
            IsActive = model.IsActive;
        }

        /// <summary>
        /// FromModel — створити новий BindRow з BindModel.
        ///
        /// Використовує UpdateFromModel, щоб:
        /// - не дублювати присвоєння
        /// - тримати одну “правду” мапінгу
        /// </summary>
        public static BindRow FromModel(BindModel model)
        {
            // Захист від null.
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            // Створюємо рядок.
            var row = new BindRow();

            // Заповнюємо.
            row.UpdateFromModel(model);

            // Повертаємо.
            return row;
        }
    }
}

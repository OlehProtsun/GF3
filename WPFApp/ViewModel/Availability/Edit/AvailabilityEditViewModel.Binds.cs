using DataAccessLayer.Models;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFApp.Infrastructure.AvailabilityMatrix;
using WPFApp.ViewModel.Availability.Helpers;

namespace WPFApp.ViewModel.Availability.Edit
{
    /// <summary>
    /// Bind-и (гарячі клавіші / вставки):
    /// - SetBinds
    /// - TryGetBindValue (через кеш)
    /// - UpsertBindAsync / FormatKeyGesture (делегування owner)
    /// - TryApplyBindToCell (з нормалізацією)
    /// - кеш + підписки на зміни
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        public void SetBinds(IEnumerable<BindModel> binds)
        {
            // 1) Відписуємось від старих BindRow.PropertyChanged, щоб не було memory leak.
            UnhookAllBindRows();

            // 2) Чистимо колекцію.
            Binds.Clear();

            // 3) Наповнюємо колекцію новими BindRow.
            foreach (var bind in binds)
                Binds.Add(BindRow.FromModel(bind));

            // 4) Підписуємось на нові BindRow.PropertyChanged.
            HookAllBindRows();

            // 5) Кеш стає неактуальним — позначаємо dirty.
            MarkBindCacheDirty();
        }

        public bool TryGetBindValue(string rawKey, out string value)
        {
            // 1) Значення за замовчуванням.
            value = string.Empty;

            // 2) Нормалізація ключа — через owner (єдина “правда” правил).
            if (!_owner.TryNormalizeKey(rawKey, out var normalizedKey))
                return false;

            // 3) Переконуємось, що кеш актуальний.
            EnsureBindCache();

            // 4) Повертаємо результат пошуку.
            return _activeBindCache.TryGetValue(normalizedKey, out value);
        }

        public Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)
            // Делегуємо owner.
            => _owner.UpsertBindAsync(bind, ct);

        public string? FormatKeyGesture(Key key, ModifierKeys modifiers)
            // Делегуємо owner.
            => _owner.FormatKeyGesture(key, modifiers);

        public bool TryApplyBindToCell(string columnName, int rowIndex, string bindValue, out int? nextRowIndex)
        {
            // 1) За замовчуванням “наступний рядок” не виставлено.
            nextRowIndex = null;

            // 2) Перевіряємо колонку:
            //    - не пусто
            //    - не Day column (її редагувати не можна)
            if (string.IsNullOrWhiteSpace(columnName) || columnName == DayColumnName)
                return false;

            // 3) Перевіряємо індекс рядка.
            if (rowIndex < 0 || rowIndex >= _groupTable.Rows.Count)
                return false;

            // 4) Нормалізуємо bindValue (якщо це інтервал).
            if (AvailabilityMatrixEngine.TryNormalizeCell(bindValue, out var normalized, out _))
                bindValue = normalized;

            // 5) Записуємо в клітинку.
            _groupTable.Rows[rowIndex][columnName] = bindValue;

            // 6) Рахуємо “наступний рядок” для UX.
            int nextRow = rowIndex + 1;

            // 7) Якщо він у межах — повертаємо.
            if (nextRow < _groupTable.Rows.Count)
                nextRowIndex = nextRow;

            return true;
        }

        // ----------------------------
        // Bind cache plumbing
        // ----------------------------

        private void Binds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 1) Будь-яка зміна колекції робить кеш неактуальним.
            MarkBindCacheDirty();

            // 2) Якщо були видалені елементи — відписуємось.
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is BindRow row)
                        UnhookBindRow(row);
                }
            }

            // 3) Якщо були додані елементи — підписуємось.
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is BindRow row)
                        HookBindRow(row);
                }
            }
        }

        private void MarkBindCacheDirty()
            // Просто ставимо прапорець.
            => _bindCacheDirty = true;

        private void EnsureBindCache()
        {
            // 1) Якщо кеш не dirty — нічого не робимо.
            if (!_bindCacheDirty)
                return;

            // 2) Чистимо кеш.
            _activeBindCache.Clear();

            // 3) Переносимо всі активні bind-и в кеш.
            foreach (var bind in Binds)
            {
                // Пропускаємо неактивні.
                if (!bind.IsActive)
                    continue;

                // Беремо key.
                var key = bind.Key ?? string.Empty;

                // Пропускаємо порожні.
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                // Кладемо у кеш.
                _activeBindCache[key] = bind.Value ?? string.Empty;
            }

            // 4) Позначаємо кеш актуальним.
            _bindCacheDirty = false;
        }

        private void HookAllBindRows()
        {
            // Підписуємось на PropertyChanged для кожного row.
            foreach (var row in Binds)
                HookBindRow(row);
        }

        private void UnhookAllBindRows()
        {
            // Відписуємось від усіх підписок, які зберігаються в словнику.
            foreach (var kv in _bindRowHandlers.ToList())
                UnhookBindRow(kv.Key);

            // Чистимо словник.
            _bindRowHandlers.Clear();
        }

        private void HookBindRow(BindRow row)
        {
            // 1) Підписуємось лише якщо BindRow підтримує INotifyPropertyChanged.
            if (row is not INotifyPropertyChanged inpc)
                return;

            // 2) Якщо підписка вже є — не додаємо ще раз.
            if (_bindRowHandlers.ContainsKey(row))
                return;

            // 3) Будь-яка зміна BindRow => кеш dirty.
            PropertyChangedEventHandler h = (_, __) => MarkBindCacheDirty();

            // 4) Підписка.
            inpc.PropertyChanged += h;

            // 5) Зберігаємо handler, щоб потім коректно відписатися.
            _bindRowHandlers[row] = h;
        }

        private void UnhookBindRow(BindRow row)
        {
            // 1) Якщо не INotifyPropertyChanged — відписуватися нема від чого.
            if (row is not INotifyPropertyChanged inpc)
                return;

            // 2) Якщо handler знайдений — відписуємось.
            if (_bindRowHandlers.TryGetValue(row, out var h))
            {
                inpc.PropertyChanged -= h;
                _bindRowHandlers.Remove(row);
            }
        }
    }
}

/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewModel.Binds у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFApp.ViewModel.Availability.Helpers;
using System.Windows;
using System.Windows.Threading;
using WPFApp.Applications.Matrix.Availability;

namespace WPFApp.ViewModel.Availability.Edit
{
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        /// <summary>
        /// Визначає публічний елемент `public void SetBinds(IEnumerable<BindModel> binds)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetBinds(IEnumerable<BindModel> binds)
        {
            
            UnhookAllBindRows();

            
            Binds.Clear();

            
            foreach (var bind in binds)
                Binds.Add(BindRow.FromModel(bind));

            
            HookAllBindRows();

            
            MarkBindCacheDirty();
        }

        /// <summary>
        /// Визначає публічний елемент `public bool TryGetBindValue(string rawKey, out string value)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool TryGetBindValue(string rawKey, out string value)
        {
            
            value = string.Empty;

            
            if (!_owner.TryNormalizeKey(rawKey, out var normalizedKey))
                return false;

            
            EnsureBindCache();

            
            return _activeBindCache.TryGetValue(normalizedKey, out value);
        }

        /// <summary>
        /// Визначає публічний елемент `public Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)
            
            => _owner.UpsertBindAsync(bind, ct);

        /// <summary>
        /// Визначає публічний елемент `public string? FormatKeyGesture(Key key, ModifierKeys modifiers)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string? FormatKeyGesture(Key key, ModifierKeys modifiers)
            
            => _owner.FormatKeyGesture(key, modifiers);

        /// <summary>
        /// Визначає публічний елемент `public bool TryApplyBindToCell(string columnName, int rowIndex, string bindValue, out int? nextRowIndex)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool TryApplyBindToCell(string columnName, int rowIndex, string bindValue, out int? nextRowIndex)
        {
            
            nextRowIndex = null;

            
            
            
            if (string.IsNullOrWhiteSpace(columnName) || columnName == DayColumnName)
                return false;

            
            if (rowIndex < 0 || rowIndex >= _groupTable.Rows.Count)
                return false;

            
            if (AvailabilityMatrixEngine.TryNormalizeCell(bindValue, out var normalized, out _))
                bindValue = normalized;

            
            _groupTable.Rows[rowIndex][columnName] = bindValue;

            
            int nextRow = rowIndex + 1;

            
            if (nextRow < _groupTable.Rows.Count)
                nextRowIndex = nextRow;

            return true;
        }

        
        
        

        private void Binds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            
            MarkBindCacheDirty();

            
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is BindRow row)
                        UnhookBindRow(row);
                }
            }

            
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
            
            => _bindCacheDirty = true;

        private void EnsureBindCache()
        {
            
            if (!_bindCacheDirty)
                return;

            
            _activeBindCache.Clear();

            
            foreach (var bind in Binds)
            {
                
                if (!bind.IsActive)
                    continue;

                
                var key = bind.Key ?? string.Empty;

                
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                
                _activeBindCache[key] = bind.Value ?? string.Empty;
            }

            
            _bindCacheDirty = false;
        }

        private void HookAllBindRows()
        {
            
            foreach (var row in Binds)
                HookBindRow(row);
        }

        private void UnhookAllBindRows()
        {
            
            foreach (var kv in _bindRowHandlers.ToList())
                UnhookBindRow(kv.Key);

            
            _bindRowHandlers.Clear();
        }

        private void HookBindRow(BindRow row)
        {
            
            if (row is not INotifyPropertyChanged inpc)
                return;

            
            if (_bindRowHandlers.ContainsKey(row))
                return;

            
            PropertyChangedEventHandler h = (_, __) => MarkBindCacheDirty();

            
            inpc.PropertyChanged += h;

            
            _bindRowHandlers[row] = h;
        }

        private void UnhookBindRow(BindRow row)
        {
            
            if (row is not INotifyPropertyChanged inpc)
                return;

            
            if (_bindRowHandlers.TryGetValue(row, out var h))
            {
                inpc.PropertyChanged -= h;
                _bindRowHandlers.Remove(row);
            }
        }


    }
}

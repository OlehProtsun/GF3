/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityEditViewModel.Batching у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Threading;

namespace WPFApp.ViewModel.Availability.Edit
{
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityEditViewModel
    {
        
        
        

        private IDisposable EnterMatrixUpdate()
        {
            
            _matrixUpdateDepth++;

            
            return new MatrixUpdateScope(this);
        }

        private sealed class MatrixUpdateScope : IDisposable
        {
            private AvailabilityEditViewModel? _vm;

            /// <summary>
            /// Визначає публічний елемент `public MatrixUpdateScope(AvailabilityEditViewModel vm)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public MatrixUpdateScope(AvailabilityEditViewModel vm)
                => _vm = vm;

            /// <summary>
            /// Визначає публічний елемент `public void Dispose()` та контракт його використання у шарі WPFApp.
            /// </summary>
            public void Dispose()
            {
                
                var vm = Interlocked.Exchange(ref _vm, null);
                if (vm is null)
                    return;

                
                vm._matrixUpdateDepth--;

                
                if (vm._matrixUpdateDepth == 0 && vm._pendingMatrixChanged)
                {
                    vm._pendingMatrixChanged = false;
                    vm.NotifyMatrixChangedCore();
                }
            }
        }

        private void NotifyMatrixChanged()
        {
            
            if (_matrixUpdateDepth > 0)
            {
                
                _pendingMatrixChanged = true;
                return;
            }

            
            NotifyMatrixChangedCore();
        }

        private void NotifyMatrixChangedCore()
        {
            
            MatrixChanged?.Invoke(this, EventArgs.Empty);

            
            
            OnPropertyChanged(nameof(AvailabilityDays));
        }

        
        
        

        private IDisposable EnterDateSync()
        {
            
            _dateSyncDepth++;

            
            return new DateSyncScope(this);
        }

        private sealed class DateSyncScope : IDisposable
        {
            private AvailabilityEditViewModel? _vm;

            /// <summary>
            /// Визначає публічний елемент `public DateSyncScope(AvailabilityEditViewModel vm)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public DateSyncScope(AvailabilityEditViewModel vm)
                => _vm = vm;

            /// <summary>
            /// Визначає публічний елемент `public void Dispose()` та контракт його використання у шарі WPFApp.
            /// </summary>
            public void Dispose()
            {
                
                var vm = Interlocked.Exchange(ref _vm, null);
                if (vm is null)
                    return;

                
                vm._dateSyncDepth--;

                
                if (vm._dateSyncDepth == 0)
                    vm.RegenerateGroupDays();
            }
        }
    }
}

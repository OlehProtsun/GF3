/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerScheduleEditViewModel.Lookups у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WPFApp.ViewModel.Container.ScheduleEdit
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerScheduleEditViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerScheduleEditViewModel
    {
        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private int _selectionSyncDepth;

        
        
        
        
        
        
        
        private readonly struct SelectionSyncScope : IDisposable
        {
            private readonly ContainerScheduleEditViewModel _vm;

            /// <summary>
            /// Визначає публічний елемент `public SelectionSyncScope(ContainerScheduleEditViewModel vm)` та контракт його використання у шарі WPFApp.
            /// </summary>
            public SelectionSyncScope(ContainerScheduleEditViewModel vm)
            {
                _vm = vm;
                _vm._selectionSyncDepth++;
            }

            /// <summary>
            /// Визначає публічний елемент `public void Dispose()` та контракт його використання у шарі WPFApp.
            /// </summary>
            public void Dispose()
            {
                
                _vm._selectionSyncDepth = Math.Max(0, _vm._selectionSyncDepth - 1);
            }
        }

        
        
        
        
        
        
        private SelectionSyncScope EnterSelectionSync() => new(this);


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetLookups(IEnumerable<ShopModel> shops,` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetLookups(IEnumerable<ShopModel> shops,
                               IEnumerable<AvailabilityGroupModel> groups,
                               IEnumerable<EmployeeModel> employees)
        {
            SetShops(shops);
            SetAvailabilityGroups(groups);
            SetEmployees(employees);
        }

        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetShops(IEnumerable<ShopModel> shops)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetShops(IEnumerable<ShopModel> shops)
        {
            using var _ = EnterSelectionSync();

            SetOptions(Shops, shops);

            
            if (SelectedBlock != null)
                SelectedShop = Shops.FirstOrDefault(s => s.Id == SelectedBlock.Model.ShopId);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetAvailabilityGroups(IEnumerable<AvailabilityGroupModel> groups)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetAvailabilityGroups(IEnumerable<AvailabilityGroupModel> groups)
        {
            using var _ = EnterSelectionSync();

            SetOptions(AvailabilityGroups, groups);

            if (SelectedBlock != null)
            {
                _suppressAvailabilityGroupUpdate = true;
                try
                {
                    SelectedAvailabilityGroup = AvailabilityGroups
                        .FirstOrDefault(g => g.Id == SelectedBlock.SelectedAvailabilityGroupId);
                }
                finally
                {
                    _suppressAvailabilityGroupUpdate = false;
                }

                
                var groupId = SelectedAvailabilityGroup?.Id ?? 0;
                if (groupId > 0)
                    SafeForget(LoadAvailabilityContextAsync(groupId));
            }
        }


        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SetEmployees(IEnumerable<EmployeeModel> employees)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SetEmployees(IEnumerable<EmployeeModel> employees)
        {
            using var _ = EnterSelectionSync();
            SetOptions(Employees, employees);
        }


        
        
        

        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void CommitPendingShopSelection()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void CommitPendingShopSelection()
        {
            ClearValidationErrors(nameof(PendingSelectedShop));
            ClearValidationErrors(nameof(ScheduleShopId));

            
            ClearValidationErrors(nameof(SelectedShop));

            if (PendingSelectedShop == SelectedShop)
                return;

            SelectedShop = PendingSelectedShop;
        }


        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void CommitPendingAvailabilitySelection()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void CommitPendingAvailabilitySelection()
        {
            ClearValidationErrors(nameof(PendingSelectedAvailabilityGroup));
            ClearValidationErrors(nameof(SelectedAvailabilityGroup)); 

            if (PendingSelectedAvailabilityGroup == SelectedAvailabilityGroup)
                return;

            SelectedAvailabilityGroup = PendingSelectedAvailabilityGroup;
        }



        
        
        

        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public void SyncSelectionFromBlock()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public void SyncSelectionFromBlock()
        {
            using var _ = EnterSelectionSync();

            if (SelectedBlock == null)
            {
                SelectedShop = null;

                _suppressAvailabilityGroupUpdate = true;
                try
                {
                    SelectedAvailabilityGroup = null;
                }
                finally
                {
                    _suppressAvailabilityGroupUpdate = false;
                }

                return;
            }

            
            SelectedShop = Shops.FirstOrDefault(s => s.Id == SelectedBlock.Model.ShopId);

            
            _suppressAvailabilityGroupUpdate = true;
            try
            {
                SelectedAvailabilityGroup = AvailabilityGroups
                    .FirstOrDefault(g => g.Id == SelectedBlock.SelectedAvailabilityGroupId);
            }
            finally
            {
                _suppressAvailabilityGroupUpdate = false;
            }
        }


        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private void SetOptions<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            
            
            if (items is not IList<T> list)
                list = items.ToList();

            
            if (target.Count == list.Count)
            {
                var same = true;

                for (var i = 0; i < list.Count; i++)
                {
                    
                    
                    
                    if (!EqualityComparer<T>.Default.Equals(target[i], list[i]))
                    {
                        same = false;
                        break;
                    }
                }

                
                if (same)
                    return;
            }

            
            target.Clear();
            foreach (var item in list)
                target.Add(item);
        }
    }
}

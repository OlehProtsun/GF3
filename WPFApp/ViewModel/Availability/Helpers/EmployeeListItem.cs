/*
  Опис файлу: цей модуль містить реалізацію компонента EmployeeListItem у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
namespace WPFApp.ViewModel.Availability.Helpers
{
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed class EmployeeListItem` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class EmployeeListItem
    {
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public int Id { get; init; }` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Id { get; init; }

        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public string FullName { get; init; } = string.Empty;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public string FullName { get; init; } = string.Empty;

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public override string ToString() => FullName;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public override string ToString() => FullName;
    }
}

/*
  Опис файлу: цей модуль містить реалізацію компонента ObservableObject у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Runtime.CompilerServices;

namespace WPFApp.MVVM.Core
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public abstract class ObservableObject : ViewModelBase` та контракт його використання у шарі WPFApp.
    /// </summary>
    public abstract class ObservableObject : ViewModelBase
    {
        
        
        
        
        protected void Raise([CallerMemberName] string? propName = null)
            => OnPropertyChanged(propName);
    }
}

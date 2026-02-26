/*
  Опис файлу: цей модуль містить реалізацію компонента NavPage у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace WPFApp.ViewModel.Main
{
    /// <summary>
    /// Визначає публічний елемент `public enum NavPage` та контракт його використання у шарі WPFApp.
    /// </summary>
    public enum NavPage
    {
        None = 0,
        Home,
        Employee,
        Shop,
        Availability,
        Container,
        Information,
        Database
    }
}

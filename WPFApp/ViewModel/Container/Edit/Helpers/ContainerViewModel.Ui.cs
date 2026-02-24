using System;
using System.Threading.Tasks;
using System.Windows;              // Application.Current.Dispatcher
using System.Windows.Media;        // Color
using WPFApp.Applications.Diagnostics;
using WPFApp.UI.Dialogs;
using WPFApp.ViewModel.Dialogs;    // CustomMessageBox, ExceptionMessageBuilder

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.Ui — частина (partial) ContainerViewModel, яка відповідає ТІЛЬКИ за UI-утиліти:
    ///
    /// 1) RunOnUiThreadAsync(Action)
    ///    - гарантує виконання Action в UI потоці (Dispatcher)
    ///
    /// 2) ShowInfo / ShowError / Confirm
    ///    - централізовані діалоги через CustomMessageBox
    ///    - щоб не дублювати “як показувати помилки” у 10 місцях
    ///
    /// 3) TryPickScheduleCellColor(...)
    ///    - виклик сервісу color picker
    ///
    /// Чому це винесено:
    /// - ці методи не відносяться до бізнес-логіки контейнерів чи schedule
    /// - вони є “інфраструктурою UI” і повинні жити окремо, щоб головний файл був чистий
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        /// <summary>
        /// Відкрити color picker і повернути обраний колір.
        ///
        /// initialColor:
        /// - колір, який ми хочемо показати “початковим” у діалозі
        ///
        /// out color:
        /// - колір, який вибрав користувач
        ///
        /// Повертає:
        /// - true  => користувач підтвердив вибір (OK)
        /// - false => користувач закрив/скасував діалог
        ///
        /// Важливо:
        /// - це просто проксі на _colorPickerService (інфраструктура/сервіс)
        /// - щоб ViewModel не знав деталей реалізації діалогу
        /// </summary>
        internal bool TryPickScheduleCellColor(Color? initialColor, out Color color)
            => _colorPickerService.TryPickColor(initialColor, out color);

        /// <summary>
        /// Запустити дію в UI thread.
        ///
        /// Навіщо:
        /// - WPF дозволяє змінювати ObservableCollection / властивості, які читає UI,
        ///   тільки з UI потоку.
        /// - Багато методів у ContainerViewModel працюють асинхронно (await + Task.Run),
        ///   тому без цього helper’а легко отримати помилки типу:
        ///     "The calling thread cannot access this object because a different thread owns it."
        ///
        /// Логіка:
        /// 1) Якщо Application/Dispatcher нема (наприклад unit-тест) — виконуємо одразу.
        /// 2) Якщо ми вже в UI thread — виконуємо одразу.
        /// 3) Інакше — Dispatcher.InvokeAsync(action).
        /// </summary>
        internal Task RunOnUiThreadAsync(Action action)
        {
            if (Application.Current?.Dispatcher is null)
            {
                action();
                return Task.CompletedTask;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        /// <summary>
        /// Показати інформаційне повідомлення користувачу.
        /// Це єдиний “стандартний” спосіб показу Info у застосунку.
        /// </summary>
        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        /// <summary>
        /// Показати помилку (рядком).
        /// </summary>
        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        /// <summary>
        /// Показати помилку (Exception) у 2 частинах:
        /// - summary: коротко “що сталося”
        /// - details: деталі (stack trace / inner exceptions) для діагностики
        ///
        /// Це корисно:
        /// - для девелопмента/сапорту
        /// - щоб у користувача було зрозуміле коротке повідомлення
        /// - але при цьому можна відкрити "details", якщо треба
        /// </summary>
        internal void ShowError(Exception ex)
        {
            var (summary, details) = ExceptionMessageBuilder.Build(ex);

            CustomMessageBox.Show(
                "Error",
                summary,
                CustomMessageBoxIcon.Error,
                okText: "OK",
                details: details);
        }

        /// <summary>
        /// Confirm — стандартне підтвердження “Так/Ні”.
        ///
        /// caption:
        /// - якщо не передали, буде "Confirm"
        ///
        /// Повертає:
        /// - true  => користувач натиснув Yes
        /// - false => користувач натиснув No / закрив вікно
        /// </summary>
        internal bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");
    }
}

using DataAccessLayer.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFApp.Infrastructure.Hotkeys;
using WPFApp.ViewModel.Availability.Helpers;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Binds — CRUD hotkey bind-ів.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        internal async Task AddBindAsync(CancellationToken ct = default)
        {
            // 1) Унікальний маркер, щоб після reload знайти саме той рядок.
            var draftKey = $"__draft__{Guid.NewGuid():N}";

            // 2) Створюємо "чернетку" в БД одразу, щоб одразу отримати реальний Id через LoadBindsAsync.
            //    Важливо:
            //    - IsActive=false, щоб чернетка не впливала на реальні хоткеї
            //    - Value може бути "" або "__draft__" — залежно від обмежень БД
            var draft = new BindModel
            {
                Key = draftKey,
                Value = string.Empty,
                IsActive = false
            };

            try
            {
                // 3) Create в БД.
                await _bindService.CreateAsync(draft, ct);

                // 4) Reload binds.
                await LoadBindsAsync(ct);
            }
            catch (Exception ex)
            {
                // 5) Помилка — показуємо.
                ShowError(ex);
                return;
            }

            // 6) Знаходимо чернетку у колекції (в EditVm).
            var row = EditVm.Binds.FirstOrDefault(b => b.Key == draftKey);
            if (row != null)
            {
                // 7) Для користувача показуємо “порожні” значення (ніби новий рядок).
                row.Key = string.Empty;
                row.Value = string.Empty;

                // 8) ТУТ є вибір UX:
                //    - якщо поставити true, рядок стає активним одразу
                //    - якщо лишити false, можна активувати лише після валідного заповнення
                row.IsActive = true;

                // 9) Виставляємо selection, щоб курсор/фокус був у новому рядку.
                EditVm.SelectedBind = row;
            }
        }

        internal async Task DeleteBindAsync(CancellationToken ct = default)
        {
            // 1) Беремо вибраний bind.
            var bind = EditVm.SelectedBind;
            if (bind is null)
                return;

            // 2) Confirm.
            if (!Confirm($"Delete bind '{bind.Key}'?", "Confirm"))
                return;

            // 3) Якщо це “локальний/тимчасовий” рядок без Id — просто прибираємо з колекції.
            if (bind.Id == 0)
            {
                EditVm.Binds.Remove(bind);
                return;
            }

            try
            {
                // 4) Delete в БД.
                await _bindService.DeleteAsync(bind.Id, ct);

                // 5) Reload binds.
                await LoadBindsAsync(ct);

                // 6) Після reload selection можна очистити (або лишити як є, залежить від UX).
                EditVm.SelectedBind = null;
            }
            catch (Exception ex)
            {
                // 7) Помилка — показуємо.
                ShowError(ex);
            }
        }

        internal async Task UpsertBindAsync(BindRow? bind, CancellationToken ct = default)
        {
            // 1) Якщо нічого не передали — виходимо.
            if (bind is null)
                return;

            // 2) Якщо користувач нічого не ввів — теж виходимо.
            if (string.IsNullOrWhiteSpace(bind.Key) && string.IsNullOrWhiteSpace(bind.Value))
                return;

            // 3) Якщо рядок не заповнений повністю — не зберігаємо і не “ругаємось” (м’яка UX-логіка).
            if (string.IsNullOrWhiteSpace(bind.Key) || string.IsNullOrWhiteSpace(bind.Value))
                return;

            // 4) Нормалізуємо key через єдиний helper.
            if (!KeyGestureTextHelper.TryNormalizeKey(bind.Key, out var normalizedKey))
            {
                ShowError("Invalid hotkey format.");
                return;
            }

            // 5) Записуємо нормалізований key назад у VM-рядок (щоб UI одразу бачив стабільний формат).
            bind.Key = normalizedKey;

            // 6) Формуємо модель.
            var model = bind.ToModel();

            // 7) Для відновлення selection після reload — запам’ятовуємо “ідентифікатор”.
            var selectId = bind.Id;            // якщо !=0
            var selectKey = normalizedKey;     // fallback якщо це create

            try
            {
                // 8) Create або Update.
                if (bind.Id == 0)
                    await _bindService.CreateAsync(model, ct);
                else
                    await _bindService.UpdateAsync(model, ct);

                // 9) Reload binds.
                await LoadBindsAsync(ct);

                // 10) Повертаємо selection:
                //     - якщо був Id — шукаємо по Id
                //     - інакше — по key (після create key вже стабільний)
                var restored = selectId > 0
                    ? EditVm.Binds.FirstOrDefault(b => b.Id == selectId)
                    : EditVm.Binds.FirstOrDefault(b => string.Equals(b.Key, selectKey, StringComparison.OrdinalIgnoreCase));

                if (restored != null)
                    EditVm.SelectedBind = restored;
            }
            catch (Exception ex)
            {
                // 11) Помилка — показуємо.
                ShowError(ex);
            }
        }

        internal string? FormatKeyGesture(Key key, ModifierKeys modifiers)
        {
            // 1) Делегуємо форматування helper-у (єдина реалізація).
            return KeyGestureTextHelper.FormatKeyGesture(key, modifiers);
        }

        internal bool TryNormalizeKey(string raw, out string normalized)
        {
            // 1) Делегуємо нормалізацію helper-у.
            return KeyGestureTextHelper.TryNormalizeKey(raw, out normalized);
        }
    }
}

using BusinessLogicLayer.Contracts.Shops;
﻿
namespace WPFApp.ViewModel.Shop.Helpers
{
    /// <summary>
    /// ShopDisplayHelper — helper для “відображення” полів Shop у read-only UI.
    /// </summary>
    public static class ShopDisplayHelper
    {
        /// <summary>
        /// Якщо значення null/whitespace — повертає "—", інакше повертає Trim().
        /// </summary>
        public static string TextOrDash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "—";

            return value.Trim();
        }

        /// <summary>
        /// Витягнути Name з null-safe.
        /// </summary>
        public static string NameOrEmpty(ShopDto? model)
            => model?.Name?.Trim() ?? string.Empty;
    }
}

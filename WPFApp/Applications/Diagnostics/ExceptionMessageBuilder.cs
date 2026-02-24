using System;
using System.Diagnostics;
using System.Text;

namespace WPFApp.Applications.Diagnostics
{
    /// <summary>
    /// ExceptionMessageBuilder — будує два рядки:
    /// - summary: короткий текст для UI (зазвичай перше повідомлення)
    /// - details: довший текст для “Details” секції (ланцюжок exception + stack trace)
    ///
    /// Оптимізації/покращення:
    /// - акуратно обробляє AggregateException (Flatten)
    /// - має обмеження глибини, щоб не залипнути на циклах/дуже глибоких ланцюгах
    /// - за замовчуванням включає stack trace у details (корисно для копіювання)
    /// </summary>
    public static class ExceptionMessageBuilder
    {
        /// <summary>
        /// Сумісний з вашим існуючим викликом API: Build(ex) -> (summary, details).
        /// </summary>
        public static (string summary, string details) Build(Exception ex)
            => Build(ex, includeStackTrace: true, maxDepth: 16, maxAggregateItems: 32);

        /// <summary>
        /// Розширена версія:
        /// includeStackTrace:
        /// - true  => додаємо StackTrace у details
        /// - false => лише типи + повідомлення
        ///
        /// maxDepth:
        /// - максимальна глибина InnerException-ланцюга
        ///
        /// maxAggregateItems:
        /// - максимум елементів AggregateException.Flatten().InnerExceptions
        /// </summary>
        public static (string summary, string details) Build(
            Exception? ex,
            bool includeStackTrace,
            int maxDepth,
            int maxAggregateItems)
        {
            // Якщо ex == null — повертаємо “невідомо”.
            if (ex is null)
                return ("Unknown error.", string.Empty);

            // Лог у Trace: корисно для девелоперів (в Output).
            // Це НЕ впливає на UI.
            Trace.WriteLine(ex.ToString());

            // summary: максимально коротко.
            var summary = string.IsNullOrWhiteSpace(ex.Message) ? "Unknown error." : ex.Message;

            // Builder з початковою ємністю, щоб зменшити realloc при великих stack trace.
            var sb = new StringBuilder(capacity: 1024);

            // Додаємо короткий заголовок для details.
            sb.AppendLine("Exception details:");
            sb.AppendLine();

            // Записуємо ланцюг / aggregate.
            AppendException(sb, ex, depth: 0, includeStackTrace, maxDepth, maxAggregateItems);

            // Повертаємо (summary, details) без зайвих trailing newline.
            return (summary, sb.ToString().TrimEnd());
        }

        private static void AppendException(
            StringBuilder sb,
            Exception ex,
            int depth,
            bool includeStackTrace,
            int maxDepth,
            int maxAggregateItems)
        {
            // Захист від занадто глибоких ланцюгів.
            if (depth > maxDepth)
            {
                sb.AppendLine("... (exception chain truncated: max depth reached)");
                return;
            }

            // Якщо це AggregateException — розгортаємо.
            if (ex is AggregateException agg)
            {
                // Flatten дає “плоский” список inner exceptions.
                var flat = agg.Flatten();

                sb.AppendLine($"AggregateException: {flat.InnerExceptions.Count} inner exception(s).");

                // Обмежуємо кількість, щоб не отримати мегатекст.
                var take = Math.Min(flat.InnerExceptions.Count, maxAggregateItems);

                for (int i = 0; i < take; i++)
                {
                    var inner = flat.InnerExceptions[i];

                    sb.AppendLine();
                    sb.AppendLine($"[Aggregate #{i + 1}] {inner.GetType().Name}: {inner.Message}");

                    // Якщо треба — додаємо stack.
                    if (includeStackTrace && !string.IsNullOrWhiteSpace(inner.StackTrace))
                    {
                        sb.AppendLine("StackTrace:");
                        sb.AppendLine(inner.StackTrace);
                    }
                }

                if (flat.InnerExceptions.Count > take)
                    sb.AppendLine($"... (aggregate truncated: showing {take} of {flat.InnerExceptions.Count})");

                return;
            }

            // Для звичайних exception: пишемо мітку рівня.
            var label = depth == 0 ? "Error" : $"Inner {depth}";
            sb.AppendLine($"{label}: {ex.GetType().Name}: {ex.Message}");

            // StackTrace (якщо доступний і requested).
            if (includeStackTrace && !string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);
            }

            // Якщо є InnerException — рекурсивно додаємо далі.
            if (ex.InnerException != null)
            {
                sb.AppendLine(); // розділювач
                AppendException(sb, ex.InnerException, depth + 1, includeStackTrace, maxDepth, maxAggregateItems);
            }
        }
    }
}

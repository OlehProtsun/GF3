using System;
using System.Text;

namespace WPFApp.Applications.Diagnostics
{
    /// <summary>
    /// Utility-клас для формування людиночитного тексту помилки з <see cref="Exception"/>.
    ///
    /// Відповідає за:
    /// - Побудову короткого резюме (summary) для UI/логів (як правило Message або fallback).
    /// - Побудову детального опису (details) для діагностики: типи винятків, повідомлення,
    ///   вкладені винятки (InnerException) та, опційно, StackTrace.
    /// - Безпечне обмеження об’єму виводу:
    ///   * maxDepth — обмежує глибину ланцюжка InnerException, щоб уникнути нескінченного/надто великого тексту.
    ///   * maxAggregateItems — обмежує кількість елементів у AggregateException, щоб лог не “роздувався”.
    ///
    /// Типовий сценарій використання:
    /// var (summary, details) = ExceptionMessageBuilder.Build(ex);
    /// summary показати у MessageBox/Toast, details — у деталях/лог-файлі.
    /// </summary>
    public static class ExceptionMessageBuilder
    {
        /// <summary>
        /// Зручне перевантаження з "дефолтними" параметрами:
        /// - includeStackTrace = true (включати StackTrace у details)
        /// - maxDepth = 16 (максимальна глибина InnerException)
        /// - maxAggregateItems = 32 (скільки inner-винятків показувати для AggregateException)
        ///
        /// Повертає кортеж:
        /// - summary: короткий опис (переважно ex.Message або "Unknown error.")
        /// - details: багаторядковий текст з діагностикою
        /// </summary>
        public static (string summary, string details) Build(Exception ex)
            => Build(ex, includeStackTrace: true, maxDepth: 16, maxAggregateItems: 32);

        /// <summary>
        /// Основний метод побудови тексту помилки.
        ///
        /// Логіка:
        /// 1) Якщо ex == null -> повертає ("Unknown error.", "").
        /// 2) summary = ex.Message (або fallback, якщо порожній).
        /// 3) details будується через <see cref="StringBuilder"/>:
        ///    - заголовок "Exception details:"
        ///    - далі рекурсивний обхід винятків (AppendException).
        ///
        /// includeStackTrace:
        /// - true: додає StackTrace (якщо він є)
        /// - false: details буде коротшим (без StackTrace)
        ///
        /// maxDepth:
        /// - обмежує рекурсію по InnerException, щоб не отримати надто великий текст/цикли.
        ///
        /// maxAggregateItems:
        /// - обмежує скільки елементів показувати для AggregateException (після Flatten()).
        /// </summary>
        public static (string summary, string details) Build(
            Exception? ex,
            bool includeStackTrace,
            int maxDepth,
            int maxAggregateItems)
        {
            // Якщо виняток не передали — повертаємо стандартний fallback.
            if (ex is null)
                return ("Unknown error.", string.Empty);

            // Короткий текст для UI: якщо Message порожній/пробіли — fallback.
            var summary = string.IsNullOrWhiteSpace(ex.Message) ? "Unknown error." : ex.Message;

            // StringBuilder для ефективного формування багаторядкового details.
            var sb = new StringBuilder(capacity: 1024);

            sb.AppendLine("Exception details:");
            sb.AppendLine();

            // Рекурсивно додаємо інформацію про виняток і його вкладені причини.
            AppendException(sb, ex, depth: 0, includeStackTrace, maxDepth, maxAggregateItems);

            // TrimEnd прибирає зайві пробіли/переноси рядків у кінці.
            return (summary, sb.ToString().TrimEnd());
        }

        /// <summary>
        /// Рекурсивно додає інформацію про виняток у StringBuilder.
        ///
        /// Підтримує два випадки:
        /// 1) AggregateException:
        ///    - Flatten() збирає всі вкладені винятки в один список
        ///    - виводить до maxAggregateItems елементів
        /// 2) Звичайний Exception:
        ///    - виводить тип + повідомлення
        ///    - опційно StackTrace
        ///    - якщо є InnerException — викликає себе рекурсивно (depth + 1)
        ///
        /// depth/maxDepth:
        /// - якщо depth > maxDepth — ланцюжок обрізається повідомленням про truncation.
        /// </summary>
        private static void AppendException(
            StringBuilder sb,
            Exception ex,
            int depth,
            bool includeStackTrace,
            int maxDepth,
            int maxAggregateItems)
        {
            // Страховка від надто глибокого ланцюжка InnerException (або теоретичних циклів).
            if (depth > maxDepth)
            {
                sb.AppendLine("... (exception chain truncated: max depth reached)");
                return;
            }

            // Спеціальна обробка AggregateException (часто виникає у Task/async або паралельних операціях).
            if (ex is AggregateException agg)
            {
                // Flatten() робить один плоский список усіх inner exceptions.
                var flat = agg.Flatten();

                sb.AppendLine($"AggregateException: {flat.InnerExceptions.Count} inner exception(s).");

                // Показуємо не більше maxAggregateItems, щоб не роздувати лог.
                var take = Math.Min(flat.InnerExceptions.Count, maxAggregateItems);

                for (int i = 0; i < take; i++)
                {
                    var inner = flat.InnerExceptions[i];

                    sb.AppendLine();
                    sb.AppendLine($"[Aggregate #{i + 1}] {inner.GetType().Name}: {inner.Message}");

                    if (includeStackTrace && !string.IsNullOrWhiteSpace(inner.StackTrace))
                    {
                        sb.AppendLine("StackTrace:");
                        sb.AppendLine(inner.StackTrace);
                    }
                }

                // Якщо елементів більше ніж показали — додаємо службове повідомлення.
                if (flat.InnerExceptions.Count > take)
                    sb.AppendLine($"... (aggregate truncated: showing {take} of {flat.InnerExceptions.Count})");

                return;
            }

            // Для першого винятку пишемо "Error", для вкладених — "Inner {depth}".
            var label = depth == 0 ? "Error" : $"Inner {depth}";
            sb.AppendLine($"{label}: {ex.GetType().Name}: {ex.Message}");

            // Додаємо StackTrace тільки якщо це дозволено і він не порожній.
            if (includeStackTrace && !string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);
            }

            // Якщо є вкладений виняток — рекурсивно додаємо його (з розділювачем для читабельності).
            if (ex.InnerException != null)
            {
                sb.AppendLine(); // розділювач
                AppendException(sb, ex.InnerException, depth + 1, includeStackTrace, maxDepth, maxAggregateItems);
            }
        }
    }
}
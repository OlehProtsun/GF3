/*
  Опис файлу: цей модуль містить реалізацію компонента ExceptionMessageBuilder у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Text;

namespace WPFApp.Applications.Diagnostics
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public static class ExceptionMessageBuilder` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class ExceptionMessageBuilder
    {
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static (string summary, string details) Build(Exception ex)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static (string summary, string details) Build(Exception ex)
            => Build(ex, includeStackTrace: true, maxDepth: 16, maxAggregateItems: 32);

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public static (string summary, string details) Build(` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static (string summary, string details) Build(
            Exception? ex,
            bool includeStackTrace,
            int maxDepth,
            int maxAggregateItems)
        {
            
            if (ex is null)
                return ("Unknown error.", string.Empty);

            
            var summary = string.IsNullOrWhiteSpace(ex.Message) ? "Unknown error." : ex.Message;

            
            var sb = new StringBuilder(capacity: 1024);

            sb.AppendLine("Exception details:");
            sb.AppendLine();

            
            AppendException(sb, ex, depth: 0, includeStackTrace, maxDepth, maxAggregateItems);

            
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
            
            if (depth > maxDepth)
            {
                sb.AppendLine("... (exception chain truncated: max depth reached)");
                return;
            }

            
            if (ex is AggregateException agg)
            {
                
                var flat = agg.Flatten();

                sb.AppendLine($"AggregateException: {flat.InnerExceptions.Count} inner exception(s).");

                
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

                
                if (flat.InnerExceptions.Count > take)
                    sb.AppendLine($"... (aggregate truncated: showing {take} of {flat.InnerExceptions.Count})");

                return;
            }

            
            var label = depth == 0 ? "Error" : $"Inner {depth}";
            sb.AppendLine($"{label}: {ex.GetType().Name}: {ex.Message}");

            
            if (includeStackTrace && !string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);
            }

            
            if (ex.InnerException != null)
            {
                sb.AppendLine(); 
                AppendException(sb, ex.InnerException, depth + 1, includeStackTrace, maxDepth, maxAggregateItems);
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Text;

namespace WPFApp.Infrastructure
{
    public static class ExceptionMessageBuilder
    {
        public static (string summary, string details) Build(Exception ex)
        {
            if (ex is null)
                return ("Unknown error.", string.Empty);

            Trace.WriteLine(ex.ToString());

            var summary = string.IsNullOrWhiteSpace(ex.Message) ? "Unknown error." : ex.Message;
            var builder = new StringBuilder();

            var depth = 0;
            var current = ex;
            while (current != null)
            {
                var label = depth == 0 ? "Error" : $"Inner {depth}";
                builder.AppendLine($"{label}: {current.GetType().Name}: {current.Message}");
                current = current.InnerException;
                depth++;
            }

            return (summary, builder.ToString().TrimEnd());
        }
    }
}

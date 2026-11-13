using System.Globalization;

namespace WebApi.Services;

internal enum EmployeePushMessageKind { Schedule, Availability, Swap, OpenShift, AcceptedSwap }

internal sealed class EmployeePushMessages(string? language)
{
    private readonly bool polish = language == "pl";
    private readonly CultureInfo culture = CultureInfo.GetCultureInfo(language == "uk" ? "uk-UA" : language == "pl" ? "pl-PL" : "en-GB");
    private static readonly TimeZoneInfo Warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    public (string Title, string Body) Format(EmployeePushMessageKind kind, string name, int year, int month,
        DateTimeOffset? deadline = null, bool closed = false, int? day = null, string? from = null, string? to = null)
    {
        // Bound user-entered context so the encrypted payload remains within the existing push limit.
        name = new string(name.EnumerateRunes().Take(24).SelectMany(x => x.ToString()).ToArray());
        var period = new DateOnly(year, month, 1).ToString("MMMM yyyy", culture);
        var context = $"„{name}” · {period}";
        var shift = day is { } d ? new DateOnly(year, month, d).ToString("dd.MM.yyyy", culture) : period;
        if (from is not null && to is not null) shift += $" · {from}–{to}";
        var due = deadline is { } end ? TimeZoneInfo.ConvertTime(end, Warsaw).ToString("dd.MM HH:mm", culture) : null;
        if (language == "uk") return kind switch
        {
            EmployeePushMessageKind.Schedule => ("Новий графік", context),
            EmployeePushMessageKind.Availability => (closed ? "Подання доступності завершено" : "Подання доступності відкрито",
                context + (closed || due is null ? "" : $" · до {due}")),
            EmployeePushMessageKind.OpenShift => ("Вільна зміна", $"„{name}” · {shift}"),
            EmployeePushMessageKind.AcceptedSwap => ("Зміну прийнято", $"„{name}” · {shift}"),
            _ => ("Доступна заміна зміни", $"„{name}” · {shift}")
        };
        return (kind, polish) switch
        {
            (EmployeePushMessageKind.Schedule, true) => ("Nowy grafik", context),
            (EmployeePushMessageKind.Schedule, false) => ("New schedule", context),
            (EmployeePushMessageKind.Availability, true) => (closed ? "Dostępność zamknięta" : "Dostępność otwarta",
                context + (closed || due is null ? "" : $" · do {due}")),
            (EmployeePushMessageKind.Availability, false) => (closed ? "Availability closed" : "Availability is open",
                context + (closed || due is null ? "" : $" · until {due}")),
            (EmployeePushMessageKind.OpenShift, true) => ("Wolna zmiana", $"„{name}” · {shift}"),
            (EmployeePushMessageKind.OpenShift, false) => ("Open shift", $"„{name}” · {shift}"),
            (EmployeePushMessageKind.AcceptedSwap, true) => ("Zmiana przyjęta", $"„{name}” · {shift}"),
            (EmployeePushMessageKind.AcceptedSwap, false) => ("Shift accepted", $"„{name}” · {shift}"),
            (_, true) => ("Dostępna zamiana zmiany", $"„{name}” · {shift}"),
            _ => ("Shift swap available", $"„{name}” · {shift}")
        };
    }
}

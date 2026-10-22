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
        name = new string(name.EnumerateRunes().Take(48).SelectMany(x => x.ToString()).ToArray());
        var period = new DateOnly(year, month, 1).ToString("MMMM yyyy", culture);
        var context = $"„{name}” · {period}";
        var shift = day is { } d ? new DateOnly(year, month, d).ToString("d MMM yyyy", culture) : period;
        if (from is not null && to is not null) shift += $" · {from}–{to}";
        var due = deadline is { } end ? TimeZoneInfo.ConvertTime(end, Warsaw).ToString("d MMM HH:mm zzz", culture) : null;
        if (language == "uk") return kind switch
        {
            EmployeePushMessageKind.Schedule => ("Опубліковано новий графік", $"{context}. Ваш робочий графік уже доступний."),
            EmployeePushMessageKind.Availability => ("Опубліковано нову доступність", closed
                ? $"{context}. Подання завершено. Відкрийте GF, щоб переглянути форму."
                : $"{context}. Уже можна вказати доступність.{(due is null ? "" : $" Подайте до {due}.")}"),
            EmployeePushMessageKind.OpenShift => ("Опубліковано вільну зміну", $"„{name}” · {shift}. Відкрийте GF, щоб переглянути подробиці."),
            EmployeePushMessageKind.AcceptedSwap => ("Вашу зміну прийнято", $"„{name}” · {shift}. Вашу пропозицію заміни прийнято."),
            _ => ("Доступна заміна зміни", $"„{name}” · {shift}. З'явилася нова пропозиція заміни. Відкрийте GF, щоб переглянути подробиці.")
        };
        return (kind, polish) switch
        {
            (EmployeePushMessageKind.Schedule, true) => ("Opublikowano nowy grafik", $"{context}. Grafik pracy jest już dostępny."),
            (EmployeePushMessageKind.Schedule, false) => ("New schedule published", $"{context}. Your work schedule is now available."),
            (EmployeePushMessageKind.Availability, true) => ("Udostępniono nową dostępność", closed
                ? $"{context}. Termin zgłaszania dostępności już minął. Otwórz GF, aby zobaczyć formularz."
                : $"{context}. Możesz już podać swoją dostępność.{(due is null ? "" : $" Termin: {due}.")}"),
            (EmployeePushMessageKind.Availability, false) => ("New availability published", closed
                ? $"{context}. Submissions have closed. Open GF to view the form."
                : $"{context}. Availability is now open.{(due is null ? "" : $" Submit by {due}.")}"),
            (EmployeePushMessageKind.OpenShift, true) => ("Opublikowano wolną zmianę", $"„{name}” · {shift}. Otwórz GF, aby zobaczyć szczegóły."),
            (EmployeePushMessageKind.OpenShift, false) => ("Open shift posted", $"„{name}” · {shift}. Open GF to view the details."),
            (EmployeePushMessageKind.AcceptedSwap, true) => ("Twoja zamiana została zaakceptowana", $"„{name}” · {shift}. Twoja propozycja zamiany została zaakceptowana."),
            (EmployeePushMessageKind.AcceptedSwap, false) => ("Your shift was accepted", $"„{name}” · {shift}. Your shift swap has been accepted."),
            (_, true) => ("Dostępna nowa zamiana", $"„{name}” · {shift}. Pojawiła się nowa propozycja zamiany. Otwórz GF, aby zobaczyć szczegóły."),
            _ => ("Shift swap available", $"„{name}” · {shift}. A new shift swap is available. Open GF to view the details.")
        };
    }
}

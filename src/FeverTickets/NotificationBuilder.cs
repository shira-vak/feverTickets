using System.Globalization;

namespace FeverTickets;

// Decides which messages to send, based on what was found. Pure logic - no sending happens here.
public static class NotificationBuilder
{
    public static IReadOnlyList<NtfyMessage> Build(IReadOnlyList<ZoneStatus> statuses, Settings settings)
    {
        List<NtfyMessage> messages = [];

        ZoneStatus[] primaryAvailable = AvailableIn(statuses, settings.PrimaryZone);
        ZoneStatus[] secondaryAvailable = AvailableIn(statuses, settings.SecondaryZone);

        // Only one "tickets" message per run: the secondary zone is only interesting if the primary is sold out.
        if (primaryAvailable.Length > 0)
        {
            messages.Add(new NtfyMessage(
                Title: $"{settings.PrimaryZone} tickets available!",
                Message: DescribeAll(primaryAvailable),
                Priority: NtfyPriority.Urgent,
                Tags: ["rotating_light", "tickets"],
                ClickUrl: settings.EventUrl));
        }
        else if (secondaryAvailable.Length > 0)
        {
            messages.Add(new NtfyMessage(
                Title: $"{settings.SecondaryZone} tickets available ({settings.PrimaryZone} still sold out)",
                Message: DescribeAll(secondaryAvailable),
                Priority: NtfyPriority.Default,
                Tags: ["tickets"],
                ClickUrl: settings.EventUrl));
        }

        // Heartbeat and debug runs both send the full status; one message even when both are on.
        if (settings.IsHeartbeat || settings.IsDebug)
        {
            messages.Add(new NtfyMessage(
                Title: settings.IsDebug ? "Debug: check result" : "Still watching",
                Message: DescribeAll(statuses),
                Priority: NtfyPriority.Low,
                Tags: ["eyes"],
                ClickUrl: null)); // Only ticket alerts link to the page.
        }

        return messages;
    }

    // One line per zone, e.g. "Zone A - Wed 4 Nov 21:00 - 12 left, 165 ILS".
    public static string Describe(ZoneStatus status)
    {
        string when = status.StartsAt.ToString(Constants.Formats.DisplayDate, CultureInfo.InvariantCulture);

        // A switch expression: the first pattern that matches wins. `{ } session` means "not null, call it session".
        string state = status.Session switch
        {
            null => "not listed on the page",
            { HasAvailableTickets: false } => "sold out",
            { } session => $"{session.AvailableTickets} left, {session.Price} {session.Currency}",
        };

        return $"{status.Zone} - {when} - {state}";
    }

    private static string DescribeAll(IEnumerable<ZoneStatus> statuses)
    {
        return string.Join("\n", statuses.Select(Describe));
    }

    private static ZoneStatus[] AvailableIn(IReadOnlyList<ZoneStatus> statuses, string zone)
    {
        // `Where` is like JS `filter`; `[.. x]` collects the result into an array.
        return [.. statuses.Where(status => status.IsAvailable && status.Zone == zone)];
    }
}

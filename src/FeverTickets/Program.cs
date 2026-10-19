// Entry point. This file uses "top-level statements": no Main method or class needed,
// the code here simply runs from top to bottom. `return` sets the process exit code.
using FeverTickets;
using static FeverTickets.Constants;

try
{
    Settings settings = Settings.FromEnvironment();

    // `using` disposes the client (closes its connections) when the program ends.
    using var httpClient = new HttpClient { Timeout = Http.Timeout };
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Http.UserAgent);

    Console.WriteLine($"Checking {settings.EventUrl}");
    string html = await httpClient.GetStringAsync(settings.EventUrl);
    IReadOnlyList<TicketSession> sessions = TicketPageParser.Parse(html);

    if (settings.IsDebug)
    {
        Console.WriteLine($"[debug] Found {sessions.Count} sessions on the page:");
        foreach (TicketSession session in sessions)
        {
            // Records print all their properties automatically: "TicketSession { StartsAt = ..., Zone = ... }".
            Console.WriteLine($"[debug]   {session}");
        }
    }

    IReadOnlyList<ZoneStatus> statuses = AvailabilityChecker.Check(sessions, settings);
    foreach (ZoneStatus status in statuses)
    {
        Console.WriteLine($"  {NotificationBuilder.Describe(status)}");
    }

    IReadOnlyList<NtfyMessage> messages = NotificationBuilder.Build(statuses, settings);
    var ntfy = new NtfyClient(httpClient, settings.NtfyTopic);
    foreach (NtfyMessage message in messages)
    {
        await ntfy.SendAsync(message);
        Console.WriteLine($"Sent notification: {message.Title}");
    }

    return ExitCodes.Success;
}
// The only place that catches every exception: print it and exit with a failure code.
// The GitHub workflow sees the failure and sends the "checker failed" ntfy alert.
catch (Exception ex)
{
    Console.Error.WriteLine($"Check failed: {ex}");
    return ExitCodes.Failure;
}

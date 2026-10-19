using System.Globalization;

namespace FeverTickets;

// What we found for one zone we are watching. Session is null when the page does not list it
// (e.g. the date already passed).
public sealed record ZoneStatus(DateTime StartsAt, string Zone, TicketSession? Session)
{
    // `=>` here defines a computed, read-only property (like a TS getter).
    // `?.` gives null instead of crashing when Session is null; `== true` turns that null into false.
    public bool IsAvailable => Session?.HasAvailableTickets == true;
}

// Matches what is on the page against what we are watching: every target session × (primary, secondary) zone.
public static class AvailabilityChecker
{
    public static IReadOnlyList<ZoneStatus> Check(IReadOnlyList<TicketSession> sessions, Settings settings)
    {
        string[] watchedZones = [settings.PrimaryZone, settings.SecondaryZone];
        List<ZoneStatus> statuses = [];

        foreach (DateTime startsAt in settings.TargetSessions)
        {
            foreach (string zone in watchedZones)
            {
                // `FirstOrDefault` is like JS `find`: the first match, or null when nothing matches.
                TicketSession? session = sessions.FirstOrDefault(candidate =>
                    candidate.StartsAt == startsAt && candidate.Zone.Equals(zone, StringComparison.OrdinalIgnoreCase));

                statuses.Add(new ZoneStatus(startsAt, zone, session));
            }
        }

        // If none of the watched sessions exist on the page at all, something is wrong (a typo in
        // TARGET_SESSIONS / zone names, or all dates already passed) - fail so we get alerted.
        if (statuses.All(status => status.Session is null))
        {
            string listed = string.Join(", ", sessions.Select(s =>
                $"{s.StartsAt.ToString(Constants.Formats.Session, CultureInfo.InvariantCulture)} {s.Zone}"));
            throw new InvalidOperationException($"None of the watched sessions are on the page. On the page: {listed}");
        }

        return statuses;
    }
}

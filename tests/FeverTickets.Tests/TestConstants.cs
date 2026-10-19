using FeverTickets;

namespace FeverTickets.Tests;

// Values that match Fixtures/event-page.html, shared by all test files.
public static class TestConstants
{
    public const string FixturePath = "Fixtures/event-page.html";
    public const string EventUrl = "https://example.test/event";
    public const string NtfyTopic = "test-topic";
    public const string PrimaryZone = "Zone A";
    public const string SecondaryZone = "Zone B";
    public const string Currency = "ILS";

    // `static readonly` instead of `const` because DateTime values cannot be compile-time constants.
    public static readonly DateTime November4 = new(2026, 11, 4, 21, 0, 0);
    public static readonly DateTime December1 = new(2026, 12, 1, 19, 0, 0);
    public static readonly DateTime NotOnPage = new(2026, 11, 20, 20, 0, 0);

    public static string ReadFixture()
    {
        return File.ReadAllText(FixturePath);
    }

    public static Settings CreateSettings(IReadOnlyList<DateTime> targetSessions)
    {
        return new Settings(
            EventUrl, targetSessions, PrimaryZone, SecondaryZone, NtfyTopic, IsHeartbeat: false, IsDebug: false);
    }

    public static TicketSession CreateSession(DateTime startsAt, string zone, int availableTickets)
    {
        return new TicketSession(startsAt, zone, availableTickets > 0, availableTickets, Price: 100, Currency);
    }
}

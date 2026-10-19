using System.Text.Json;
using static FeverTickets.Constants;

namespace FeverTickets;

// One option in the ticket selector: a specific date + time + zone.
public sealed record TicketSession(
    DateTime StartsAt,
    string Zone,
    bool HasAvailableTickets,
    int AvailableTickets,
    decimal Price,
    string Currency);

// Reads the ticket options out of the event page HTML.
//
// The page is server-rendered: all the data the ticket selector shows is embedded in the HTML inside
// <script id="serverapp-state" type="application/json">{...}</script>. So instead of clicking buttons in a
// browser, we read that JSON directly. The selector data is a tree: dates → times → zones (the leaves).
public static class TicketPageParser
{
    public static IReadOnlyList<TicketSession> Parse(string html)
    {
        try
        {
            // `using` disposes the document (frees its memory buffers) when the method ends.
            using JsonDocument document = JsonDocument.Parse(ExtractStateJson(html));
            JsonElement selector = FindSelector(document.RootElement);

            List<TicketSession> sessions = [];
            CollectSessions(selector.GetProperty("level"), sessions);
            return sessions;
        }
        // `when (...)` is an exception filter: only these exception types are caught; anything else keeps going up.
        // They are what JsonDocument/GetProperty throw when the JSON is not shaped the way we expect.
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or FormatException
                                       or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Could not read the ticket data - the page structure may have changed. {ex.Message}", ex);
        }
    }

    private static string ExtractStateJson(string html)
    {
        int start = html.IndexOf(FeverPage.StateScriptStart, StringComparison.Ordinal);
        int end = start < 0 ? -1 : html.IndexOf(FeverPage.ScriptEnd, start, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException("The page has no serverapp-state script.");
        }

        // `html[a..b]` is a range: the substring from index a up to (not including) b. Like JS slice(a, b).
        return html[(start + FeverPage.StateScriptStart.Length)..end];
    }

    private static JsonElement FindSelector(JsonElement state)
    {
        // The key is "LEVEL_TICKET_SELECTOR_SESSIONS.{...some options...}", so match on its start.
        foreach (JsonProperty property in state.EnumerateObject())
        {
            if (property.Name.StartsWith(FeverPage.SelectorKeyPrefix, StringComparison.Ordinal))
            {
                return property.Value;
            }
        }

        throw new InvalidOperationException($"No '{FeverPage.SelectorKeyPrefix}' entry found in the page data.");
    }

    // Walks the tree recursively: an item with a nested "level" object is a date or time group,
    // an item whose "level" is null is an actual zone you can buy.
    private static void CollectSessions(JsonElement level, List<TicketSession> sessions)
    {
        foreach (JsonElement item in level.GetProperty("items").EnumerateArray())
        {
            JsonElement childLevel = item.GetProperty("level");

            if (childLevel.ValueKind == JsonValueKind.Object)
            {
                CollectSessions(childLevel, sessions);
            }
            else
            {
                sessions.Add(ToTicketSession(item.GetProperty("value")));
            }
        }
    }

    private static TicketSession ToTicketSession(JsonElement value)
    {
        return new TicketSession(
            // `.DateTime` keeps the event's local clock time (e.g. 21:00 Israel time) and drops the offset,
            // so the result never depends on the machine's time zone (GitHub runners use UTC).
            StartsAt: value.GetProperty("starts_at_iso").GetDateTimeOffset().DateTime,
            // GetString() may return null; `??` means "if null, use the right side instead".
            Zone: value.GetProperty("label").GetString() ?? string.Empty,
            HasAvailableTickets: value.GetProperty("has_available_tickets").GetBoolean(),
            AvailableTickets: value.GetProperty("available_tickets").GetInt32(),
            Price: value.GetProperty("price").GetDecimal(),
            Currency: value.GetProperty("currency").GetString() ?? string.Empty);
    }
}

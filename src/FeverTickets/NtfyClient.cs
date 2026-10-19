using System.Net.Http.Json;

namespace FeverTickets;

// An enum is a fixed set of named values. ntfy priorities are numbers, so each name is given its number.
public enum NtfyPriority
{
    Low = 2,
    Default = 3,
    Urgent = 5,
}

// One notification. `Tags` are ntfy emoji names (e.g. "eyes" shows 👀).
// `ClickUrl` opens when you tap the notification; `string?` means it may be null = no link.
public sealed record NtfyMessage(
    string Title,
    string Message,
    NtfyPriority Priority,
    IReadOnlyList<string> Tags,
    string? ClickUrl);

// Sends notifications through ntfy.sh. Anyone subscribed to the topic (phone app or browser) receives them.
//
// The part in parentheses after the class name is a "primary constructor": the parameters are available
// inside the whole class, so there is no need to write a constructor that copies them into fields.
public sealed class NtfyClient(HttpClient httpClient, string topic)
{
    public async Task SendAsync(NtfyMessage message)
    {
        // `new { ... }` creates an anonymous object (like a TS object literal) - exactly the JSON ntfy expects.
        // Enums are written to JSON as their number, so Priority becomes e.g. 5.
        var payload = new
        {
            topic,
            title = message.Title,
            message = message.Message,
            priority = message.Priority,
            tags = message.Tags,
            click = message.ClickUrl,
        };

        // `await` works like in TS: wait for the Task (C#'s Promise) to finish.
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(Constants.Ntfy.Url, payload);
        response.EnsureSuccessStatusCode();
    }
}

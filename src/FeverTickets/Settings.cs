using System.Globalization;
// `using static` lets this file write `Defaults.PrimaryZone` instead of `Constants.Defaults.PrimaryZone`.
using static FeverTickets.Constants;

namespace FeverTickets;

// A `record` is a class made for holding data: the compiler generates the constructor,
// read-only properties, equality and ToString() for us. Like a readonly TS type with a constructor.
public sealed record Settings(
    string EventUrl,
    IReadOnlyList<DateTime> TargetSessions,
    string PrimaryZone,
    string SecondaryZone,
    string NtfyTopic,
    bool IsHeartbeat,
    bool IsDebug)
{
    // `static` = called on the type itself (Settings.FromEnvironment()), not on an instance.
    public static Settings FromEnvironment()
    {
        LoadLocalEnvFile();

        return new Settings(
            EventUrl: ReadOrDefault(EnvVars.EventUrl, Defaults.EventUrl),
            TargetSessions: ParseSessions(ReadOrDefault(EnvVars.TargetSessions, Defaults.TargetSessions)),
            PrimaryZone: ReadOrDefault(EnvVars.PrimaryZone, Defaults.PrimaryZone),
            SecondaryZone: ReadOrDefault(EnvVars.SecondaryZone, Defaults.SecondaryZone),
            NtfyTopic: ReadRequired(EnvVars.NtfyTopic),
            IsHeartbeat: ReadOrDefault(EnvVars.Heartbeat, "false").Equals("true", StringComparison.OrdinalIgnoreCase),
            IsDebug: ParseIsDebug(ReadOrDefault(EnvVars.LogLevel, Defaults.LogLevel)));
    }

    // Turns "2026-11-04 21:00, 2026-11-12 19:00" into a list of DateTime values.
    public static IReadOnlyList<DateTime> ParseSessions(string value)
    {
        string[] parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            throw new InvalidOperationException($"{EnvVars.TargetSessions} is empty - expected at least one session.");
        }

        // `[.. x]` spreads a sequence into a new list. LINQ's `Select` is the C# version of JS `map`.
        return [.. parts.Select(ParseSession)];
    }

    // LOG_LEVEL is "info" (default) or "debug".
    public static bool ParseIsDebug(string logLevel)
    {
        // A switch expression: compares the value against each case and returns the matching result.
        return logLevel.ToUpperInvariant() switch
        {
            "INFO" => false,
            "DEBUG" => true,
            _ => throw new InvalidOperationException(
                $"Invalid {EnvVars.LogLevel} '{logLevel}' - expected 'info' or 'debug'."),
        };
    }

    private static DateTime ParseSession(string session)
    {
        // TryParseExact returns true/false instead of throwing. The parsed value comes back through
        // the `out` parameter - a way for a C# method to return a second value.
        bool isValid = DateTime.TryParseExact(
            session, Formats.Session, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed);

        return isValid
            ? parsed
            : throw new InvalidOperationException(
                $"Invalid session '{session}' in {EnvVars.TargetSessions} - expected format '{Formats.Session}'.");
    }

    // GitHub passes an empty string for a repository variable that is not defined, so empty counts as "not set".
    private static string ReadOrDefault(string name, string defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static string ReadRequired(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Environment variable {name} is required but not set.")
            : value.Trim();
    }

    private static void LoadLocalEnvFile()
    {
        if (!File.Exists(LocalEnvFileName))
        {
            return;
        }

        foreach (string line in File.ReadAllLines(LocalEnvFileName))
        {
            // Split only on the first '=' so values may contain '=' themselves.
            string[] parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            bool isSetting = parts.Length == 2 && !parts[0].StartsWith('#');

            // A real environment variable wins over the file, so one value can still be overridden per run.
            if (isSetting && Environment.GetEnvironmentVariable(parts[0]) is null)
            {
                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }
}

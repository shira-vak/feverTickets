namespace FeverTickets;

// Every fixed value in the script, grouped by topic. The classes are nested, so a value is used as e.g.
// `Constants.Defaults.PrimaryZone`, or just `Defaults.PrimaryZone` in files with `using static FeverTickets.Constants;`.
public static class Constants
{
    // Names of the environment variables (GitHub secrets/variables, or lines in `.env` locally).
    public static class EnvVars
    {
        public const string EventUrl = "EVENT_URL";
        public const string TargetSessions = "TARGET_SESSIONS";
        public const string PrimaryZone = "PRIMARY_ZONE";
        public const string SecondaryZone = "SECONDARY_ZONE";
        public const string NtfyTopic = "NTFY_TOPIC";
        public const string Heartbeat = "HEARTBEAT";
        public const string LogLevel = "LOG_LEVEL";
    }

    // Used when the matching environment variable is not set.
    public static class Defaults
    {
        public const string EventUrl = "https://feverup.com/m/436641/en";
        public const string TargetSessions = "2026-11-04 21:00, 2026-11-12 19:00";
        public const string PrimaryZone = "Zone A";
        public const string SecondaryZone = "Zone B";
        public const string LogLevel = "info";
    }

    public static class Formats
    {
        // A session in TARGET_SESSIONS and in log messages, in the event's local time (Israel).
        public const string Session = "yyyy-MM-dd HH:mm";

        // How a session is shown in notifications, e.g. "Wed 4 Nov 21:00".
        public const string DisplayDate = "ddd d MMM HH:mm";
    }

    // Where the ticket data sits inside the Fever event page HTML.
    public static class FeverPage
    {
        public const string StateScriptStart = "<script id=\"serverapp-state\" type=\"application/json\">";
        public const string ScriptEnd = "</script>";
        public const string SelectorKeyPrefix = "LEVEL_TICKET_SELECTOR_SESSIONS";
    }

    public static class Http
    {
        public const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36";

        // Generous on purpose: the Fever page is ~900 KB and sometimes takes 30+ seconds to download.
        // `static readonly` instead of `const`: a TimeSpan is created at runtime, so it can't be a compile-time const.
        public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);
    }

    public static class Ntfy
    {
        // ntfy accepts a JSON message posted to its root URL, with the topic inside the JSON.
        public const string Url = "https://ntfy.sh/";
    }

    public static class ExitCodes
    {
        public const int Success = 0;
        public const int Failure = 1;
    }

    // For local runs: KEY=VALUE lines in this file become environment variables. Git-ignored; absent on GitHub.
    public const string LocalEnvFileName = ".env";
}

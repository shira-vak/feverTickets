using FeverTickets;

namespace FeverTickets.Tests;

public sealed class SettingsTests
{
    [Fact]
    public void ParseSessions_WhenListIsValid_ShouldReturnEverySession()
    {
        IReadOnlyList<DateTime> sessions = Settings.ParseSessions("2026-11-04 21:00, 2026-12-01 19:00");

        Assert.Equal([TestConstants.November4, TestConstants.December1], sessions);
    }

    [Theory]
    [InlineData("")]
    [InlineData("04/11/2026 21:00")]
    [InlineData("2026-11-04")]
    public void ParseSessions_WhenValueIsInvalid_ShouldThrow(string value)
    {
        Assert.Throws<InvalidOperationException>(() => Settings.ParseSessions(value));
    }

    [Theory]
    [InlineData("info", false)]
    [InlineData("debug", true)]
    [InlineData("DEBUG", true)]
    public void ParseIsDebug_WhenValueIsKnown_ShouldReturnMode(string value, bool expected)
    {
        Assert.Equal(expected, Settings.ParseIsDebug(value));
    }

    [Fact]
    public void ParseIsDebug_WhenValueIsUnknown_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => Settings.ParseIsDebug("verbose"));
    }
}

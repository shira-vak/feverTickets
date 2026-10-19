using FeverTickets;

namespace FeverTickets.Tests;

public sealed class NotificationBuilderTests
{
    private const int SomeTickets = 5;
    private const int NoTickets = 0;

    private readonly Settings _settings = TestConstants.CreateSettings([TestConstants.November4]);

    [Fact]
    public void Build_WhenPrimaryAvailable_ShouldSendOnlyUrgentMessage()
    {
        IReadOnlyList<ZoneStatus> statuses = CreateStatuses(primaryTickets: SomeTickets, secondaryTickets: SomeTickets);

        IReadOnlyList<NtfyMessage> messages = NotificationBuilder.Build(statuses, _settings);

        NtfyMessage message = Assert.Single(messages);
        Assert.Equal(NtfyPriority.Urgent, message.Priority);
        Assert.Equal(TestConstants.EventUrl, message.ClickUrl);
    }

    [Fact]
    public void Build_WhenOnlySecondaryAvailable_ShouldSendDefaultPriorityMessage()
    {
        IReadOnlyList<ZoneStatus> statuses = CreateStatuses(primaryTickets: NoTickets, secondaryTickets: SomeTickets);

        IReadOnlyList<NtfyMessage> messages = NotificationBuilder.Build(statuses, _settings);

        NtfyMessage message = Assert.Single(messages);
        Assert.Equal(NtfyPriority.Default, message.Priority);
    }

    [Fact]
    public void Build_WhenNothingAvailable_ShouldSendNothing()
    {
        IReadOnlyList<ZoneStatus> statuses = CreateStatuses(primaryTickets: NoTickets, secondaryTickets: NoTickets);

        IReadOnlyList<NtfyMessage> messages = NotificationBuilder.Build(statuses, _settings);

        Assert.Empty(messages);
    }

    [Fact]
    public void Build_WhenHeartbeat_ShouldSendDefaultPriorityStatusMessage()
    {
        IReadOnlyList<ZoneStatus> statuses = CreateStatuses(primaryTickets: NoTickets, secondaryTickets: NoTickets);
        // `with` copies a record and changes only the listed properties.
        Settings heartbeatSettings = _settings with { IsHeartbeat = true };

        IReadOnlyList<NtfyMessage> messages = NotificationBuilder.Build(statuses, heartbeatSettings);

        NtfyMessage message = Assert.Single(messages);
        Assert.Equal(NtfyPriority.Default, message.Priority);
        Assert.Contains("sold out", message.Message, StringComparison.Ordinal);
        Assert.Null(message.ClickUrl);
    }

    [Fact]
    public void Build_WhenDebugAndNothingAvailable_ShouldStillSendStatusMessage()
    {
        IReadOnlyList<ZoneStatus> statuses = CreateStatuses(primaryTickets: NoTickets, secondaryTickets: NoTickets);
        Settings debugSettings = _settings with { IsDebug = true };

        IReadOnlyList<NtfyMessage> messages = NotificationBuilder.Build(statuses, debugSettings);

        NtfyMessage message = Assert.Single(messages);
        Assert.StartsWith("Debug", message.Title, StringComparison.Ordinal);
        Assert.Equal(NtfyPriority.Low, message.Priority);
    }

    [Fact]
    public void Build_WhenDebugAndHeartbeat_ShouldSendOneHeartbeatMessage()
    {
        IReadOnlyList<ZoneStatus> statuses = CreateStatuses(primaryTickets: NoTickets, secondaryTickets: NoTickets);
        Settings settings = _settings with { IsDebug = true, IsHeartbeat = true };

        IReadOnlyList<NtfyMessage> messages = NotificationBuilder.Build(statuses, settings);

        NtfyMessage message = Assert.Single(messages);
        Assert.Equal("Still watching", message.Title);
        Assert.Equal(NtfyPriority.Default, message.Priority);
    }

    private static ZoneStatus[] CreateStatuses(int primaryTickets, int secondaryTickets)
    {
        DateTime startsAt = TestConstants.November4;
        return
        [
            new ZoneStatus(startsAt, TestConstants.PrimaryZone,
                TestConstants.CreateSession(startsAt, TestConstants.PrimaryZone, primaryTickets)),
            new ZoneStatus(startsAt, TestConstants.SecondaryZone,
                TestConstants.CreateSession(startsAt, TestConstants.SecondaryZone, secondaryTickets)),
        ];
    }
}

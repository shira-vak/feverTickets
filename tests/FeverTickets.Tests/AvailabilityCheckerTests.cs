using FeverTickets;

namespace FeverTickets.Tests;

public sealed class AvailabilityCheckerTests
{
    private readonly IReadOnlyList<TicketSession> _sessions = TicketPageParser.Parse(TestConstants.ReadFixture());

    [Fact]
    public void Check_WhenTargetIsListed_ShouldReturnStatusForPrimaryAndSecondaryZones()
    {
        Settings settings = TestConstants.CreateSettings([TestConstants.November4]);

        IReadOnlyList<ZoneStatus> statuses = AvailabilityChecker.Check(_sessions, settings);

        Assert.Collection(
            statuses,
            primary => Assert.False(primary.IsAvailable),
            secondary => Assert.True(secondary.IsAvailable));
    }

    [Fact]
    public void Check_WhenOneTargetIsMissing_ShouldMarkItNotListed()
    {
        Settings settings = TestConstants.CreateSettings([TestConstants.December1, TestConstants.NotOnPage]);

        IReadOnlyList<ZoneStatus> statuses = AvailabilityChecker.Check(_sessions, settings);

        Assert.All(statuses.Where(status => status.StartsAt == TestConstants.NotOnPage),
            status => Assert.Null(status.Session));
    }

    [Fact]
    public void Check_WhenNoTargetIsListed_ShouldThrow()
    {
        Settings settings = TestConstants.CreateSettings([TestConstants.NotOnPage]);

        Assert.Throws<InvalidOperationException>(() => AvailabilityChecker.Check(_sessions, settings));
    }
}

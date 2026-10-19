using FeverTickets;

namespace FeverTickets.Tests;

public sealed class TicketPageParserTests
{
    [Fact]
    public void Parse_WhenPageHasSelectorData_ShouldReturnEveryZoneOfEveryDate()
    {
        IReadOnlyList<TicketSession> sessions = TicketPageParser.Parse(TestConstants.ReadFixture());

        Assert.Equal(3, sessions.Count);
        Assert.Contains(new TicketSession(
            TestConstants.November4, TestConstants.PrimaryZone, HasAvailableTickets: false, AvailableTickets: 0,
            Price: 175, TestConstants.Currency), sessions);
        Assert.Contains(new TicketSession(
            TestConstants.December1, TestConstants.PrimaryZone, HasAvailableTickets: true, AvailableTickets: 30,
            Price: 165, TestConstants.Currency), sessions);
    }

    // [Theory] + [InlineData] runs the same test once per data row.
    [Theory]
    [InlineData("<html><body>No state here</body></html>")]
    [InlineData("<script id=\"serverapp-state\" type=\"application/json\">{\"experiments\":[]}</script>")]
    [InlineData("<script id=\"serverapp-state\" type=\"application/json\">{not json</script>")]
    public void Parse_WhenPageStructureChanged_ShouldThrow(string html)
    {
        Assert.Throws<InvalidOperationException>(() => TicketPageParser.Parse(html));
    }
}

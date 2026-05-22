using FluentAssertions;
using QuotesApi.Services;

namespace Quotes.Tests.Unit;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsUtcOffset()
    {
        var clock = new SystemClock();
        clock.UtcNow.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void UtcNow_IsCloseToSystemTime()
    {
        var clock = new SystemClock();
        var diff = Math.Abs((clock.UtcNow - DateTimeOffset.UtcNow).TotalSeconds);
        diff.Should().BeLessThan(5);
    }
}

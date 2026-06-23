using CampLedger.Services;

namespace CampLedger.Tests;

public sealed class TripDurationValidationServiceTests
{
    [Fact]
    public void IsValid_WithSameDayDates_ReturnsFalse()
    {
        var sut = new TripDurationValidationService();

        var actual = sut.IsValid(new DateTime(2026, 6, 23), new DateTime(2026, 6, 23));

        Assert.False(actual);
    }

    [Fact]
    public void IsValid_WithMultiDayDates_ReturnsTrue()
    {
        var sut = new TripDurationValidationService();

        var actual = sut.IsValid(new DateTime(2026, 6, 23), new DateTime(2026, 6, 24));

        Assert.True(actual);
    }
}

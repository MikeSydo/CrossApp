using Core.Reports;

namespace Core.Tests;

public sealed class ReportServiceTests
{
    [Theory] // Звіт на кількох наборах
    [InlineData(1000, 2)] // Замовлення з сумою < 1000: O-1 (500) та O-4 (200) -> 2
    [InlineData(100, 0)]  // Замовлення з сумою < 100: немає -> 0
    public void LowTotal_CountsOnlyBelowThreshold(decimal threshold, int expectedCount)
    {
        var result = ReportService.LowTotal(TestData.Orders(), threshold);

        Assert.Equal(expectedCount, result.Count);
        Assert.All(result, r => Assert.True(r.Total < threshold));
    }

    [Fact] // Детермінований топ-N
    public void TopByTotal_IsDeterministic()
    {
        var result = ReportService.TopByTotal(TestData.Orders(), 2);

        Assert.Equal(["O-3", "O-2"], result.Select(o => o.Id));
    }
}

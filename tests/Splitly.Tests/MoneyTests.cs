using Splitly.Api.Domain;

namespace Splitly.Tests;

public class MoneyTests
{
    [Fact]
    public void FromDecimal_TwoDecimals_RoundTrips()
    {
        var money = Money.FromDecimal(12.34m).Value;

        Assert.Equal(1234, money.Cents);
        Assert.Equal(12.34m, money.Value);
    }

    [Fact]
    public void FromDecimal_MoreThanTwoDecimals_ReturnsError()
    {
        var result = Money.FromDecimal(10.005m);

        Assert.True(result.IsError);
        Assert.Equal(MoneyErrors.PrecisionTooHigh, result.FirstError);
    }

    [Fact]
    public void SplitEvenly_IndivisibleAmount_SharesSumToOriginal()
    {
        var money = Money.FromDecimal(100m).Value;

        var shares = money.SplitEvenly(3);

        Assert.Equal(3, shares.Count);
        Assert.Equal(money, shares.Aggregate(Money.Zero, (sum, share) => sum + share));
    }

    [Fact]
    public void SplitEvenly_RemainderGoesToFirstShares()
    {
        var money = Money.FromDecimal(100m).Value;

        var shares = money.SplitEvenly(3);

        Assert.Equal(33.34m, shares[0].Value);
        Assert.Equal(33.33m, shares[1].Value);
        Assert.Equal(33.33m, shares[2].Value);
    }
}

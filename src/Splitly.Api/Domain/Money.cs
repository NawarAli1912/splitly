using System.Globalization;
using ErrorOr;

namespace Splitly.Api.Domain;

public readonly record struct Money : IComparable<Money>
{
    public long Cents { get; }

    private Money(long cents) => Cents = cents;

    public static readonly Money Zero = new(0);

    public static ErrorOr<Money> FromDecimal(decimal amount)
    {
        var cents = amount * 100;

        if (decimal.Truncate(cents) != cents)
        {
            return MoneyErrors.PrecisionTooHigh;
        }

        return new Money((long)cents);
    }

    public static Money FromCents(long cents) => new(cents);

    public decimal Value => Cents / 100m;

    public bool IsZero => Cents == 0;

    public bool IsPositive => Cents > 0;

    public bool IsNegative => Cents < 0;

    public Money Abs() => new(Math.Abs(Cents));

    public IReadOnlyList<Money> SplitEvenly(int ways)
    {
        var baseShare = Cents / ways;
        var remainder = Cents % ways;

        var shares = new Money[ways];

        for (var i = 0; i < ways; i++)
        {
            shares[i] = new Money(baseShare + (i < remainder ? 1 : 0));
        }

        return shares;
    }

    public static Money Min(Money left, Money right) => left.Cents <= right.Cents ? left : right;

    public static Money operator +(Money left, Money right) => new(left.Cents + right.Cents);

    public static Money operator -(Money left, Money right) => new(left.Cents - right.Cents);

    public static bool operator <(Money left, Money right) => left.Cents < right.Cents;

    public static bool operator >(Money left, Money right) => left.Cents > right.Cents;

    public static bool operator <=(Money left, Money right) => left.Cents <= right.Cents;

    public static bool operator >=(Money left, Money right) => left.Cents >= right.Cents;

    public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

    public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);
}

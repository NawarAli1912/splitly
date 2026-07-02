using ErrorOr;

namespace Splitly.Api.Domain;

public static class MoneyErrors
{
    public static readonly Error PrecisionTooHigh = Error.Validation(
        "Money.PrecisionTooHigh", "Amounts cannot have more than 2 decimal places.");
}

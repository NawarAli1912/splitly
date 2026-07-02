using ErrorOr;

namespace Splitly.Api.Domain;

public static class ExpenseGroupErrors
{
    public static readonly Error NameRequired = Error.Validation(
        "ExpenseGroup.NameRequired", "Group name is required.");

    public static readonly Error InvalidCurrency = Error.Validation(
        "ExpenseGroup.InvalidCurrency", "Currency must be a 3-letter ISO code.");

    public static readonly Error ParticipantNameRequired = Error.Validation(
        "ExpenseGroup.ParticipantNameRequired", "Participant name is required.");

    public static readonly Error DuplicateParticipant = Error.Conflict(
        "ExpenseGroup.DuplicateParticipant", "A participant with this name already exists.");

    public static readonly Error ParticipantNotFound = Error.NotFound(
        "ExpenseGroup.ParticipantNotFound", "Participant is not part of this group.");

    public static readonly Error ParticipantHasExpenses = Error.Conflict(
        "ExpenseGroup.ParticipantHasExpenses", "Participant is involved in expenses and cannot be removed.");

    public static readonly Error InvalidAmount = Error.Validation(
        "ExpenseGroup.InvalidAmount", "Expense amount must be positive.");

    public static readonly Error EmptySplit = Error.Validation(
        "ExpenseGroup.EmptySplit", "An expense must be split among at least one participant.");

    public static readonly Error ExpenseNotFound = Error.NotFound(
        "ExpenseGroup.ExpenseNotFound", "Expense not found in this group.");
}

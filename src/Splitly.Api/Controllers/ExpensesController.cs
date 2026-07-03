using Microsoft.AspNetCore.Mvc;
using Splitly.Api.Application.Expenses;
using Splitly.Api.Contracts.Common;
using Splitly.Api.Contracts.Expenses;

namespace Splitly.Api.Controllers;

[Route("groups/{groupId:guid}/expenses")]
public sealed class ExpensesController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddExpense(
        Guid groupId,
        AddExpenseRequest request,
        [FromServices] AddExpenseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, request, cancellationToken);

        return result.Match(
            expense => Created($"/groups/{groupId}/expenses", expense),
            Problem);
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<ExpenseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListExpenses(
        Guid groupId,
        [FromServices] ListExpensesHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await handler.HandleAsync(groupId, page, pageSize, cancellationToken);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{expenseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveExpense(
        Guid groupId,
        Guid expenseId,
        [FromServices] RemoveExpenseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, expenseId, cancellationToken);

        return result.Match(_ => NoContent(), Problem);
    }
}

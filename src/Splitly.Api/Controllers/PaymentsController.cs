using Microsoft.AspNetCore.Mvc;
using Splitly.Api.Application.Payments;
using Splitly.Api.Contracts.Common;
using Splitly.Api.Contracts.Payments;

namespace Splitly.Api.Controllers;

[Route("groups/{groupId:guid}/payments")]
public sealed class PaymentsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordPayment(
        Guid groupId,
        RecordPaymentRequest request,
        [FromServices] RecordPaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, request, cancellationToken);

        return result.Match(
            payment => Created($"/groups/{groupId}/payments", payment),
            Problem);
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<PaymentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListPayments(
        Guid groupId,
        [FromServices] ListPaymentsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await handler.HandleAsync(groupId, page, pageSize, cancellationToken);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{paymentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePayment(
        Guid groupId,
        Guid paymentId,
        [FromServices] RemovePaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, paymentId, cancellationToken);

        return result.Match(_ => NoContent(), Problem);
    }
}

using Splitly.Api.Contracts.Payments;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Payments;

public static class PaymentMappings
{
    public static PaymentResponse ToResponse(this Payment payment) =>
        new(
            payment.Id,
            payment.FromParticipantId,
            payment.ToParticipantId,
            payment.Amount.Value,
            payment.PaidOn);
}

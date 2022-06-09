using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Mappers.PaymentStatus
{
    public interface IPaymentStatusMapper
    {
        PaymentStatusEnum MapToPaymentStatus(MercadoPagoPaymentStatusEnum status);
    }
}

using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Mappers.PaymentStatus
{
    public class PaymentStatusMapper : IPaymentStatusMapper
    {
        public PaymentStatusEnum MapToPaymentStatus(MercadoPagoPaymentStatusEnum status)
        {
            return status switch
            {
                MercadoPagoPaymentStatusEnum.Approved or MercadoPagoPaymentStatusEnum.Authorized => PaymentStatusEnum.Approved,
                MercadoPagoPaymentStatusEnum.InMediation or MercadoPagoPaymentStatusEnum.InProcess or MercadoPagoPaymentStatusEnum.Pending => PaymentStatusEnum.Pending,
                _ => PaymentStatusEnum.DeclinedPaymentTransaction,
            };
        }
    }
}

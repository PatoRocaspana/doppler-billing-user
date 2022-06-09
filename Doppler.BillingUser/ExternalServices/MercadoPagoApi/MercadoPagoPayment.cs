using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public class MercadoPagoPayment
    {
        public long Id { get; set; }
        public MercadoPagoPaymentStatusEnum Status { get; set; }
        public string StatusDetail { get; set; }
    }
}

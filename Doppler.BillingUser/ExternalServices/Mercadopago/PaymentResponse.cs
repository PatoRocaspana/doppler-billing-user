namespace Doppler.BillingUser.ExternalServices.Mercadopago
{
    public class PaymentResponse
    {
        public long? Id { get; set; }
        public string Status { get; set; }
        public string StatusDetail { get; set; }
    }
}

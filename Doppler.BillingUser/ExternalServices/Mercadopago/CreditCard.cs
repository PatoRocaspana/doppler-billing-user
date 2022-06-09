namespace Doppler.BillingUser.ExternalServices.Mercadopago
{
    public class CreditCard
    {
        public string CardNumber { get; set; }
        public PaymentCardholder Cardholder { get; set; }
        public string ExpirationYear { get; set; }
        public string ExpirationMonth { get; set; }
        public string SecurityCode { get; set; }
    }
}

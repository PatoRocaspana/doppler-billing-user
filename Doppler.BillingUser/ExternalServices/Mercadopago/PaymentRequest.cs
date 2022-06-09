namespace Doppler.BillingUser.ExternalServices.Mercadopago
{
    public class PaymentRequest
    {
        public decimal TransactionAmount { get; set; }
        public string TransactionDescription { get; set; }
        public CreditCard Card { get; set; }
        public int Installments { get; set; }
        public string PaymentMethodId { get; set; }
        public string Description { get; set; }
    }
}

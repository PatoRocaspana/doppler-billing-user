namespace Doppler.BillingUser.ExternalServices.Mercadopago
{
    public class MercadopagoApiError
    {
        public int? Status { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }
}

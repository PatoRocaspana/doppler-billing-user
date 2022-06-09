using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Mercadopago
{
    public interface IMercadopagoService
    {
        Task<PaymentResponse> CreatePayment(string accountName, decimal chargeTotal, CreditCard creditCard, string cardType);
    }
}

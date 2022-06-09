using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public interface IMercadoPagoService
    {
        Task<MercadoPagoPayment> GetPaymentById(long id, string accountname);
    }
}

using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.Mercadopago
{
    public class MercadopagoService : IMercadopagoService
    {
        private readonly IOptions<MercadopagoSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly ICurrentRequestApiTokenGetter _usersApiTokenGetter;
        private readonly IEncryptionService _encryptionService;

        public MercadopagoService(
            IOptions<MercadopagoSettings> options,
            ILogger<MercadopagoService> logger,
            IFlurlClientFactory flurlClientFac,
            ICurrentRequestApiTokenGetter usersApiTokenGetter,
            IEncryptionService encryptionService)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_options.Value.PaymentUrl);
            _usersApiTokenGetter = usersApiTokenGetter;
            _encryptionService = encryptionService;
        }

        public async Task<PaymentResponse> CreatePayment(string accountName, decimal chargeTotal, CreditCard creditCard, string cardType)
        {
            try
            {
                var paymentRequest = new PaymentRequest
                {
                    TransactionAmount = chargeTotal,
                    Description = "Doppler payment",
                    Installments = 1,
                    PaymentMethodId = cardType,
                    TransactionDescription = "MERPAGO*DOPPLER",
                    Card = new CreditCard
                    {
                        CardNumber = _encryptionService.DecryptAES256(creditCard.CardNumber),
                        Cardholder = new PaymentCardholder
                        {
                            Name = _encryptionService.DecryptAES256(creditCard.Cardholder.Name),
                            Identification = creditCard.Cardholder.Identification
                        },
                        ExpirationMonth = creditCard.ExpirationMonth,
                        ExpirationYear = creditCard.ExpirationYear,
                        SecurityCode = _encryptionService.DecryptAES256(creditCard.SecurityCode),
                    }
                };

                var paymentResponse = await _flurlClient.Request(new UriTemplate(_options.Value.PaymentUrl)
                        .AddParameter("accountname", accountName)
                        .Resolve())
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .PostJsonAsync(paymentRequest);

                return await paymentResponse.GetJsonAsync<PaymentResponse>();
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<MercadopagoApiError>();
                throw new DopplerApplicationException(Enums.PaymentErrorCode.ServerPaymentTransactionError, error.Message.Replace(" ", "_"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error mercadopago payement for user {accountName}.");
                throw;
            }
        }
    }
}

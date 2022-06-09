using Doppler.BillingUser.Authorization;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IOptions<MercadoPagoSettings> _options;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IFlurlClient _flurlClient;
        private readonly ILogger<MercadoPagoService> _logger;

        public MercadoPagoService(
            IOptions<MercadoPagoSettings> options,
            IJwtTokenGenerator jwtTokenGenerator,
            IFlurlClientFactory flurlClientFactory,
            ILogger<MercadoPagoService> logger)
        {
            _options = options;
            _jwtTokenGenerator = jwtTokenGenerator;
            _flurlClient = flurlClientFactory.Get(_options.Value.MercadoPagoApiUrlTemplate);
            _logger = logger;
        }

        public async Task<MercadoPagoPayment> GetPaymentById(long id, string accountname)
        {
            try
            {
                var payment = await _flurlClient.Request(new UriTemplate(_options.Value.MercadoPagoApiUrlTemplate)
                    .AddParameter("accountname", accountname)
                    .AddParameter("id", id)
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                    .GetJsonAsync<MercadoPagoPayment>();
                return payment;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get payment for user: {accountname} with payment ID: {id}");
                throw;
            }
        }
    }
}

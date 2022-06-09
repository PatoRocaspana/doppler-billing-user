using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using Newtonsoft.Json;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http.Testing;
using Microsoft.Extensions.Options;
using Xunit;
using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Test
{
    public class PutCurrentPaymentMethodTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private readonly WebApplicationFactory<Startup> _factory;

        public PutCurrentPaymentMethodTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task PUT_Current_payment_method_should_be_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payment-methods/current")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_method_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payment-methods/current");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_CC_method_should_update_right_value_based_on_body_information()
        {
            // Arrange
            var currentPaymentMethod = new PaymentMethod
            {
                CCHolderFullName = "Test Holder Name",
                CCNumber = "5555 5555 5555 5555",
                CCVerification = "222",
                CCExpMonth = "12",
                CCExpYear = "25",
                CCType = "Mastercard",
                PaymentMethodName = "CC",
                IdSelectedPlan = 13
            };

            var user = new User
            {
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(currentPaymentMethod), Encoding.UTF8, "application/json");

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.IsValidCreditCard(It.IsAny<ExternalServices.FirstData.CreditCard>(), It.IsAny<int>())).ReturnsAsync(true);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendUserDataToSap(It.IsAny<SapBusinessPartner>(), null));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payment-methods/current")
            {
                Headers =
                {
                    {
                        "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}"
                    }
                },
                Content = requestContent
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_Transfer_method_should_update_right_value_based_on_body_information()
        {
            // Arrange
            const int userId = 1;
            const int expectedRows = 1;

            var currentPaymentMethod = new PaymentMethod
            {
                PaymentMethodName = "TRANSF",
                IdSelectedPlan = 13,
                RazonSocial = "test",
                IdConsumerType = "RI",
                IdentificationNumber = "2334345566"
            };

            var user = new User
            {
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(currentPaymentMethod), Encoding.UTF8, "application/json");

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null)).ReturnsAsync(userId);
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendUserDataToSap(It.IsAny<SapBusinessPartner>(), null));

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payment-methods/current")
            {
                Headers =
                {
                    {
                        "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}"
                    }
                },
                Content = requestContent
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_Transfer_method_should_not_sent_to_sap_with_right_value_based_on_body_information()
        {
            // Arrange
            const int userId = 1;
            const int expectedRows = 1;

            var currentPaymentMethod = new
            {
                PaymentMethodName = "TRANSF",
                IdSelectedPlan = 13,
                RazonSocial = "test",
                IdConsumerType = "RI",
                IdentificationNumber = "2334345566"
            };

            var user = new User
            {
                Email = "test1@example.com",
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}",
                CUIT = "2334345566",
                IdConsumerType = 2,
                IdResponsabileBilling = 9,
                FirstName = "firstName"
            };

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null)).ReturnsAsync(userId);
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(GetSapSettingsMock().Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");
            var httpTest = new HttpTest();
            const string url = "https://localhost:5000/businesspartner/createorupdatebusinesspartner";

            // Act
            var response = await client.PutAsJsonAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            httpTest.ShouldNotHaveCalled(url);
        }

        [Fact]
        public async Task PUT_Current_payment_CC_method_should_return_bad_request_when_CC_is_invalid()
        {
            // Arrange
            const int userId = 1;

            var currentPaymentMethod = new PaymentMethod
            {
                CCHolderFullName = "Test Holder Name",
                CCNumber = "5555 5555 5555 5555",
                CCVerification = "222",
                CCExpMonth = "12",
                CCExpYear = "25",
                CCType = "Mastercard",
                PaymentMethodName = "CC",
                IdSelectedPlan = 13
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(currentPaymentMethod), Encoding.UTF8, "application/json");

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null))
                .ReturnsAsync(userId);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.IsValidCreditCard(It.IsAny<ExternalServices.FirstData.CreditCard>(), It.IsAny<int>())).ReturnsAsync(false);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(GetSapSettingsMock().Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payment-methods/current")
            {
                Headers =
                {
                    {
                        "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}"
                    }
                },
                Content = requestContent
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_Mercadopago_method_should_update_right_value_based_on_body_information()
        {
            // Arrange
            var currentPaymentMethod = new
            {
                CCHolderFullName = "Test Holder Name",
                CCNumber = "5555 5555 5555 5555",
                CCVerification = "222",
                CCExpMonth = "12",
                CCExpYear = "25",
                CCType = "Mastercard",
                PaymentMethodName = PaymentMethodEnum.MP.ToString(),
                IdSelectedPlan = 13,
                IdentificationNumber = "2334345566"
            };

            var user = new User
            {
                Email = "test1@example.com",
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}",
                CUIT = "2334345566",
                IdConsumerType = 2,
                IdResponsabileBilling = (int)ResponsabileBillingEnum.Mercadopago,
                FirstName = "firstName"
            };

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(GetSapSettingsMock().Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");
            var httpTest = new HttpTest();
            const string url = "https://localhost:5000/businesspartner/createorupdatebusinesspartner";

            // Act
            var response = await client.PutAsJsonAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            httpTest.ShouldHaveCalled(url);
        }

        private static Mock<IOptions<SapSettings>> GetSapSettingsMock()
        {
            var accountPlansSettingsMock = new Mock<IOptions<SapSettings>>();
            accountPlansSettingsMock.Setup(x => x.Value)
                .Returns(new SapSettings
                {
                    SapBaseUrl = "https://localhost:5000/",
                    SapCreateBusinessPartnerEndpoint = "businesspartner/createorupdatebusinesspartner"
                });

            return accountPlansSettingsMock;
        }
    }
}

using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using Newtonsoft.Json;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class PutBillingControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private readonly WebApplicationFactory<Startup> _factory;

        public PutBillingControllerTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task PUT_Billing_information_should_be_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/billing-information")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_billing_information_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/billing-information");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_billing_information_should_update_right_value_based_on_body_information()
        {
            // Arrange
            const int expectedRows = 1;

            var contactInformation = new BillingInformation
            {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test ProvinceAh pe",
                ZipCode = "ZipCode",
                Country = "Test Country"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(contactInformation), Encoding.UTF8, "application/json");

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingInformation("test1@example.com"))
                .ReturnsAsync(contactInformation);

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/billing-information")
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
        public async Task PUT_billing_information_with_different_countrye_should_update_right_value_based_on_body_information_and_also_update_payment_method()
        {
            // Arrange
            const int expectedRows = 1;

            var contactInformation = new BillingInformation
            {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test ProvinceAh pe",
                ZipCode = "ZipCode",
                Country = "Test Country"
            };

            var currentBillingInformation = new BillingInformation
            {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test ProvinceAh pe",
                ZipCode = "ZipCode",
                Country = "co"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(contactInformation), Encoding.UTF8, "application/json");

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingInformation("test1@example.com"))
                .ReturnsAsync(currentBillingInformation);
            billingRepositoryMock.Setup(x => x.GetCurrentPaymentMethod("test1@example.com"))
                .ReturnsAsync(new PaymentMethod { PaymentMethodName = PaymentMethodEnum.TRANSF.ToString() });

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es",
                IdUser = 1
            });

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/billing-information")
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
    }
}

using Dapper;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Moq.Dapper;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Doppler.BillingUser.Encryption;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class GetInvoiceRecipientsTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";

        public GetInvoiceRecipientsTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GET_Invoice_email_recipients_should_return_emails_recipients_correctly()
        {
            // Arrange
            var user = new User
            {
                BillingEmails = "test@example.com, test2@example.com"
            };
            const string expectedContent = "{\"recipients\":[\"test@example.com\",\"test2@example.com\"]}";

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null))
                .ReturnsAsync(user);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/billing-information/invoice-recipients")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContent, responseContent);
        }

        [Fact]
        public async Task GET_Invoice_email_recipients_should_return_empty_value_when_emails_recipients_is_null()
        {
            // Arrange
            var user = new User
            {
                BillingEmails = null
            };
            const string expectedContent = "{\"recipients\":[]}";

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null))
                .ReturnsAsync(user);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/billing-information/invoice-recipients")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContent, responseContent);
        }

        [Fact(Skip = "Mock dapper with null is not working https://github.com/UnoSD/Moq.Dapper/issues/51")]
        public async Task GET_Invoice_email_recipients_should_return_not_found_when_user_is_invalid()
        {
            // Arrange
            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null))
                .ReturnsAsync(null as User);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/billing-information/invoice-recipients")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Doppler.BillingUser.Test
{
    public class GetBillingInformationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        const string TOKEN_SUPERUSER_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc1NVIjp0cnVlLCJleHAiOjIwMDAwMDAwMDB9.rUtvRqMxrnQzVHDuAjgWa2GJAJwZ-wpaxqdjwP7gmVa7XJ1pEmvdTMBdirKL5BJIE7j2_hsMvEOKUKVjWUY-IE0e0u7c82TH0l_4zsIztRyHMKtt9QE9rBRQnJf8dcT5PnLiWkV_qEkpiIKQ-wcMZ1m7vQJ0auEPZyyFBKmU2caxkZZOZ8Kw_1dx-7lGUdOsUYad-1Rt-iuETGAFijQrWggcm3kV_KmVe8utznshv2bAdLJWydbsAUEfNof0kZK5Wu9A80DJd3CRiNk8mWjQxF_qPOrGCANOIYofhB13yuYi48_8zVPYku-llDQjF77BmQIIIMrCXs8IMT3Lksdxuw";
        const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";

        private readonly WebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper _output;

        public GetBillingInformationTest(WebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
        }

        [Fact]
        public async Task GET_billing_information_should_return_not_found_when_empty_db_result()
        {
            // Arrange

            var mockConnection = new Mock<DbConnection>();

            // TODO: validate input
            mockConnection.SetupDapperAsync(c => c.QueryAsync<BillingInformation>(null, null, null, null, null)).ReturnsAsync(Enumerable.Empty<BillingInformation>());
            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/billing-information")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task GET_billing_information_should_return_unauthorized_when_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/billing-information")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/accounts/test1@example.com/billing-information", TOKEN_SUPERUSER_EXPIRE_20330518, HttpStatusCode.OK)]
        [InlineData("/accounts/test1@example.com/billing-information", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518, HttpStatusCode.OK)]
        public async Task GET_billing_information_should_accept_valid_token_with_isSU_flag_or_a_token_for_the_right_account(string url, string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var dbResponse = new[] { new BillingInformation {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test Province",
                ZipCode = "Test ZipCode",
                Country = "Test Country"
            } };

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryAsync<BillingInformation>(null, null, null, null, null)).ReturnsAsync(dbResponse);
            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Fact]
        public async Task GET_billing_information_should_return_right_value_based_on_db_response()
        {
            // Arrange
            var dbResponse = new[] { new BillingInformation {
                Firstname = "Test First Name",
                Lastname = "Test Last Name",
                Phone = "5555555",
                Address = "Test Address",
                City = "Test City",
                Province = "Test Province",
                ZipCode = "Test ZipCode",
                Country = "Test Country"
            } };

            var expectedContent = "{\"firstname\":\"Test First Name\",\"lastname\":\"Test Last Name\",\"address\":\"Test Address\",\"city\":\"Test City\",\"province\":\"Test Province\",\"country\":\"Test Country\",\"zipCode\":\"Test ZipCode\",\"phone\":\"5555555\"}";

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryAsync<BillingInformation>(null, null, null, null, null)).ReturnsAsync(dbResponse);
            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/billing-information")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine(response.GetHeadersAsString());

            // Assert
            Assert.Equal(expectedContent, responseContent);
        }
    }
}

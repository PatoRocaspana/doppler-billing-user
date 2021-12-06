using System.Net.Http;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.Model;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class AccountPlansServiceTest
    {
        private readonly Mock<IOptions<AccountPlansSettings>> _accountPlansSettingsMock;

        public AccountPlansServiceTest()
        {
            _accountPlansSettingsMock = new Mock<IOptions<AccountPlansSettings>>();
            _accountPlansSettingsMock.Setup(x => x.Value)
                .Returns(new AccountPlansSettings
                {
                    CalculateUrlTemplate = "https://localhost:5000/accounts/{accountname}/newplan/{planId}/calculate?discountId={discountId}"
                });
        }

        [Fact]
        public async Task Get_account_plans_total_return_true_when_total_amount_is_equal_that_current_total_agreement()
        {
            // Arrange
            var agreement = new AgreementInformation
            {
                Total = 2,
                PlanId = 1,
                DiscountId = 3
            };
            var accountname = "test@mail.com";

            var factory = new PerBaseUrlFlurlClientFactory();
            var service = new AccountPlansService(
                _accountPlansSettingsMock.Object,
                Mock.Of<ILogger<AccountPlansService>>(),
                factory,
                Mock.Of<IUsersApiTokenGetter>());

            // Act
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { total = 2 });

            var isValid = await service.IsValidTotal(accountname, agreement);
            const string url = "https://localhost:5000/accounts/test%40mail.com/newplan/1/calculate?discountId=3";

            // Assert
            Assert.True(isValid);

            httpTest
                .ShouldHaveCalled(url)
                .WithVerb(HttpMethod.Get);
        }

        [Fact]
        public async Task Get_account_plans_total_return_false_when_total_amount_is_not_equal_that_current_total_agreement()
        {
            // Arrange
            var agreement = new AgreementInformation
            {
                Total = 2,
                PlanId = 1,
                DiscountId = 3
            };
            var accountname = "test@mail.com";

            var factory = new PerBaseUrlFlurlClientFactory();
            var service = new AccountPlansService(
                _accountPlansSettingsMock.Object,
                Mock.Of<ILogger<AccountPlansService>>(),
                factory,
                Mock.Of<IUsersApiTokenGetter>());


            // Act
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { Total = 3 });

            var isValid = await service.IsValidTotal(accountname, agreement);
            const string url = "https://localhost:5000/accounts/test%40mail.com/newplan/1/calculate?discountId=3";

            // Assert
            Assert.False(isValid);

            httpTest
                .ShouldHaveCalled(url)
                .WithVerb(HttpMethod.Get)
                .Times(1);
        }
    }
}
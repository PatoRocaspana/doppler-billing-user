using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public interface IEmailTemplatesService
    {
        Task<bool> SendNotificationForSuscribersPlan(string accountname, User userInformation, UserTypePlanInformation newPlan);
        Task<bool> SendActivatedStandByEmail(string language, string fistName, int standByAmount, string sendTo);
        Task SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation, bool isUpgradePending);
        Task SendNotificationForCredits(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode, bool isUpgradePending);
        Task SendNotificationForFailedCreditCardTransaction(int userId, string errorCode, string errorMessage, string transactionCTR, string bankMessage);
        Task SendNotificationForMercadoPagoPaymentApproved(int userId, string accountname);
    }
}

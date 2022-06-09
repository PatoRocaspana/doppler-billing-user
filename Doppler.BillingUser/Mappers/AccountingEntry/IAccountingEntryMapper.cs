using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers
{
    public interface IAccountingEntryMapper
    {
        Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, UserTypePlanInformation newPlan, CreditCardPayment payment);
        Task<AccountingEntry> MapToPaymentAccountingEntry(AccountingEntry invoiceEntry, CreditCard encryptedCreditCard);

    }
}

using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers
{
    public class AccountingEntryForMercadopagoMapper : IAccountingEntryMapper
    {
        private readonly ICurrencyRepository _currencyRepository;

        private const string AccountingEntryTypeDescriptionInvoice = "Invoice";
        private const int UserAccountType = 1;
        private const string AccountEntryTypeInvoice = "I";
        private const string AccountingEntryTypeDescriptionMpPayment = "MP Payment";
        private const string AccountEntryTypePayment = "P";
        private const string PaymentEntryTypePayment = "P";

        public AccountingEntryForMercadopagoMapper(ICurrencyRepository currencyRepository)
        {
            _currencyRepository = currencyRepository;
        }

        public async Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, UserTypePlanInformation newPlan, CreditCardPayment payment)
        {
            decimal rate = 1;
            decimal invoiceTaxes = 0;

            if (total != 0)
            {
                rate = await _currencyRepository.GetCurrencyRateAsync((int)CurrencyTypeEnum.UsS, (int)CurrencyTypeEnum.sARG, DateTime.UtcNow);
                decimal amount = await _currencyRepository.ConvertCurrencyAsync((int)CurrencyTypeEnum.UsS, (int)CurrencyTypeEnum.sARG, total, DateTime.UtcNow, rate);
                decimal taxes = CalculateInvoiceTaxes(amount);
                invoiceTaxes = await _currencyRepository.ConvertCurrencyAsync((int)CurrencyTypeEnum.sARG, (int)CurrencyTypeEnum.UsS, taxes, DateTime.UtcNow, (1 / rate));
            }
            else
            {
                payment.Status = PaymentStatusEnum.Approved;
            }

            return new AccountingEntry
            {
                IdClient = user.IdUser,
                IdCurrencyType = (int)CurrencyTypeEnum.sARG,
                CurrencyRate = rate,
                Taxes = invoiceTaxes,
                Amount = total,
                Date = DateTime.UtcNow,
                Status = payment.Status,
                Source = SourceTypeHelper.SourceTypeEnumMapper(newPlan),
                AccountingTypeDescription = AccountingEntryTypeDescriptionInvoice,
                InvoiceNumber = 0,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = (int)InvoiceBillingTypeEnum.MERCADOPAGO,
                AuthorizationNumber = payment.AuthorizationNumber,
                AccountEntryType = AccountEntryTypeInvoice
            };
        }

        public Task<AccountingEntry> MapToPaymentAccountingEntry(AccountingEntry invoiceEntry, CreditCard encryptedCreditCard)
        {
            return Task.FromResult(new AccountingEntry
            {
                IdClient = invoiceEntry.IdClient,
                Amount = invoiceEntry.Amount,
                CcCNumber = encryptedCreditCard.Number,
                CcExpMonth = encryptedCreditCard.ExpirationMonth,
                CcExpYear = encryptedCreditCard.ExpirationYear,
                CcHolderName = encryptedCreditCard.HolderName,
                Date = DateTime.Now,
                Source = invoiceEntry.Source,
                AccountEntryType = AccountEntryTypePayment,
                AuthorizationNumber = invoiceEntry.AuthorizationNumber,
                PaymentEntryType = PaymentEntryTypePayment,
                AccountingTypeDescription = AccountingEntryTypeDescriptionMpPayment,
                IdAccountType = invoiceEntry.IdAccountType,
                IdCurrencyType = invoiceEntry.IdCurrencyType,
                CurrencyRate = invoiceEntry.CurrencyRate,
                Taxes = invoiceEntry.Taxes,
                IdInvoiceBillingType = invoiceEntry.IdInvoiceBillingType
            });
        }

        private static decimal CalculateInvoiceTaxes(decimal amount)
        {
            decimal coefficient = 0.21m;
            return amount * coefficient;
        }
    }
}

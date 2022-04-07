using System;
using Doppler.BillingUser.DopplerSecurity;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using FluentValidation;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Encryption;
using System.Linq;
using Doppler.BillingUser.ExternalServices.Slack;
using Microsoft.Extensions.Options;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.Utils;
using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.ExternalServices.Zoho.API;
using Newtonsoft.Json;
using System.Collections.Generic;
using Doppler.BillingUser.Services;

namespace Doppler.BillingUser.Controllers
{
    [Authorize]
    [ApiController]
    public class BillingController
    {
        private readonly ILogger _logger;
        private readonly IBillingRepository _billingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<BillingInformation> _billingInformationValidator;
        private readonly IAccountPlansService _accountPlansService;
        private readonly IValidator<AgreementInformation> _agreementInformationValidator;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly ISapService _sapService;
        private readonly IEncryptionService _encryptionService;
        private readonly IOptions<SapSettings> _sapSettings;
        private readonly IPromotionRepository _promotionRepository;
        private readonly ISlackService _slackService;
        private readonly IOptions<ZohoSettings> _zohoSettings;
        private readonly IZohoService _zohoService;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore
        };
        private static readonly List<UserTypeEnum> AllowedPlanTypesForBilling = new List<UserTypeEnum>
        {
            UserTypeEnum.INDIVIDUAL,
            UserTypeEnum.MONTHLY,
            UserTypeEnum.SUBSCRIBERS
        };
        private static readonly List<PaymentMethodEnum> AllowedPaymentMethodsForBilling = new List<PaymentMethodEnum>
        {
            PaymentMethodEnum.CC,
            PaymentMethodEnum.TRANSF
        };

        private static readonly List<CountryEnum> AllowedCountriesForTransfer = new List<CountryEnum>
        {
            CountryEnum.Colombia,
            CountryEnum.Mexico
        };

        public BillingController(
            ILogger<BillingController> logger,
            IBillingRepository billingRepository,
            IUserRepository userRepository,
            IValidator<BillingInformation> billingInformationValidator,
            IValidator<AgreementInformation> agreementInformationValidator,
            IAccountPlansService accountPlansService,
            IPaymentGateway paymentGateway,
            ISapService sapService,
            IEncryptionService encryptionService,
            IOptions<SapSettings> sapSettings,
            IPromotionRepository promotionRepository,
            ISlackService slackService,
            IEmailSender emailSender,
            IOptions<EmailNotificationsConfiguration> emailSettings,
            IOptions<ZohoSettings> zohoSettings,
            IZohoService zohoService,
            IEmailTemplatesService emailTemplatesService)
        {
            _logger = logger;
            _billingRepository = billingRepository;
            _userRepository = userRepository;
            _billingInformationValidator = billingInformationValidator;
            _agreementInformationValidator = agreementInformationValidator;
            _accountPlansService = accountPlansService;
            _paymentGateway = paymentGateway;
            _sapService = sapService;
            _emailSender = emailSender;
            _emailSettings = emailSettings;
            _encryptionService = encryptionService;
            _sapSettings = sapSettings;
            _promotionRepository = promotionRepository;
            _slackService = slackService;
            _zohoSettings = zohoSettings;
            _zohoService = zohoService;
            _emailTemplatesService = emailTemplatesService;
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountName}/billing-information")]
        public async Task<IActionResult> GetBillingInformation(string accountName)
        {
            var billingInformation = await _billingRepository.GetBillingInformation(accountName);

            if (billingInformation == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(billingInformation);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billing-information")]
        public async Task<IActionResult> UpdateBillingInformation(string accountname, [FromBody] BillingInformation billingInformation)
        {
            var results = await _billingInformationValidator.ValidateAsync(billingInformation);
            if (!results.IsValid)
            {
                return new BadRequestObjectResult(results.ToString("-"));
            }

            await _billingRepository.UpdateBillingInformation(accountname, billingInformation);

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> GetInvoiceRecipients(string accountname)
        {
            var result = await _billingRepository.GetInvoiceRecipients(accountname);

            if (result == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(result);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> UpdateInvoiceRecipients(string accountname, [FromBody] InvoiceRecipients invoiceRecipients)
        {
            await _billingRepository.UpdateInvoiceRecipients(accountname, invoiceRecipients.Recipients, invoiceRecipients.PlanId);

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> GetCurrentPaymentMethod(string accountname)
        {
            _logger.LogDebug("Get current payment method.");

            var currentPaymentMethod = await _billingRepository.GetCurrentPaymentMethod(accountname);

            if (currentPaymentMethod == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(currentPaymentMethod);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> UpdateCurrentPaymentMethod(string accountname, [FromBody] PaymentMethod paymentMethod)
        {
            _logger.LogDebug("Update current payment method.");

            User userInformation = await _userRepository.GetUserInformation(accountname);
            var isSuccess = await _billingRepository.UpdateCurrentPaymentMethod(userInformation, paymentMethod);

            if (!isSuccess)
            {
                var messageError = $"Failed at updating payment method for user {accountname}";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
                return new BadRequestObjectResult("Failed at updating payment");
            }

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/plans/current")]
        public async Task<IActionResult> GetCurrentPlan(string accountname)
        {
            _logger.LogDebug("Get current plan.");

            var currentPlan = await _billingRepository.GetCurrentPlan(accountname);

            if (currentPlan == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(currentPlan);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPost("/accounts/{accountname}/agreements")]
        public async Task<IActionResult> CreateAgreement([FromRoute] string accountname, [FromBody] AgreementInformation agreementInformation)
        {
            try
            {
                var results = await _agreementInformationValidator.ValidateAsync(agreementInformation);
                if (!results.IsValid)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Validation error {results.ToString("-")}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult(results.ToString("-"));
                }

                var user = await _userRepository.GetUserBillingInformation(accountname);
                if (user == null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new NotFoundObjectResult("Invalid user");
                }

                if (!AllowedPaymentMethodsForBilling.Any(p => p == user.PaymentMethod))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid payment method {user.PaymentMethod}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                if (user.PaymentMethod == PaymentMethodEnum.TRANSF && !AllowedCountriesForTransfer.Any(p => (int)p == user.IdBillingCountry))
                {
                    var messageErrorTransference = $"Failed at creating new agreement for user {accountname}, payment method {user.PaymentMethod} it's only supported for {AllowedCountriesForTransfer.Select(p => p)}";
                    _logger.LogError(messageErrorTransference);
                    await _slackService.SendNotification(messageErrorTransference);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                var currentPlan = await _userRepository.GetUserCurrentTypePlan(user.IdUser);
                if (currentPlan != null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid user type (only free users) {currentPlan.IdUserType}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid user type (only free users)");
                }

                var newPlan = await _userRepository.GetUserNewTypePlan(agreementInformation.PlanId);
                if (newPlan == null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid selected plan {agreementInformation.PlanId}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid selected plan");
                }

                if (!AllowedPlanTypesForBilling.Any(p => p == newPlan.IdUserType))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, invalid selected plan type {newPlan.IdUserType}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid selected plan type");
                }

                var isValidTotal = await _accountPlansService.IsValidTotal(accountname, agreementInformation);
                if (!isValidTotal)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Total of agreement is not valid";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Total of agreement is not valid");
                }

                Promotion promotion = null;
                if (!string.IsNullOrEmpty(agreementInformation.Promocode))
                {
                    promotion = await _accountPlansService.GetValidPromotionByCode(agreementInformation.Promocode, agreementInformation.PlanId);
                }

                int invoiceId = 0;
                string authorizationNumber = string.Empty;
                CreditCard encryptedCreditCard = null;

                if (agreementInformation.Total.GetValueOrDefault() > 0 && user.PaymentMethod == PaymentMethodEnum.CC)
                {
                    encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                    if (encryptedCreditCard == null)
                    {
                        var messageError = $"Failed at creating new agreement for user {accountname}, missing credit card information";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new ObjectResult("User credit card missing")
                        {
                            StatusCode = 500
                        };
                    }

                    // TODO: Deal with first data exceptions.
                    authorizationNumber = await _paymentGateway.CreateCreditCardPayment(agreementInformation.Total.GetValueOrDefault(), encryptedCreditCard, user.IdUser);
                    invoiceId = await _billingRepository.CreateAccountingEntriesAsync(agreementInformation, encryptedCreditCard, user.IdUser, newPlan, authorizationNumber);
                }

                var billingCreditId = await _billingRepository.CreateBillingCreditAsync(agreementInformation, user, newPlan, promotion);

                user.IdCurrentBillingCredit = billingCreditId;
                user.OriginInbound = agreementInformation.OriginInbound;
                user.UpgradePending = BillingHelper.IsUpgradePending(user, promotion);
                user.UTCFirstPayment = !user.UpgradePending ? DateTime.UtcNow : null;
                user.UTCUpgrade = user.UTCFirstPayment;

                if (newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS && newPlan.SubscribersQty.HasValue)
                    user.MaxSubscribers = newPlan.SubscribersQty.Value;

                await _userRepository.UpdateUserBillingCredit(user);

                var partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                User userInformation = await _userRepository.GetUserInformation(accountname);

                if (newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                {
                    await _billingRepository.UpdateUserSubscriberLimitsAsync(user.IdUser);
                    var activatedStandByAmount = await _billingRepository.ActivateStandBySubscribers(user.IdUser);
                    if (activatedStandByAmount > 0)
                    {
                        var lang = userInformation.Language ?? "en";
                        await _emailTemplatesService.SendActivatedStandByEmail(lang, userInformation.FirstName, activatedStandByAmount, user.Email);
                    }
                }
                else
                {
                    await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan);
                }

                if (promotion != null)
                    await _promotionRepository.IncrementUsedTimes(promotion);

                if (agreementInformation.Total.GetValueOrDefault() > 0 && user.PaymentMethod == PaymentMethodEnum.CC)
                {
                    var billingCredit = await _billingRepository.GetBillingCredit(billingCreditId);

                    await _sapService.SendBillingToSap(
                        BillingHelper.MapBillingToSapAsync(_sapSettings.Value,
                            _encryptionService.DecryptAES256(encryptedCreditCard.Number),
                            _encryptionService.DecryptAES256(encryptedCreditCard.HolderName),
                            billingCredit,
                            currentPlan,
                            newPlan,
                            authorizationNumber,
                            invoiceId),
                        accountname);
                }

                //Send notifications
                SendNotifications(accountname, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId);

                var message = $"Successful at creating a new agreement for: User: {accountname} - Plan: {agreementInformation.PlanId}";
                await _slackService.SendNotification(message + (!string.IsNullOrEmpty(agreementInformation.Promocode) ? $" - Promocode {agreementInformation.Promocode}" : string.Empty));

                if (_zohoSettings.Value.UseZoho)
                {
                    ZohoDTO zohoDto = new ZohoDTO()
                    {
                        Email = user.Email,
                        UpgradeDate = DateTime.UtcNow,
                        FirstPaymentDate = DateTime.UtcNow,
                        Doppler = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL ? "Individual" : "Monthly", // TODO: check for other plan types
                        BillingSystem = PaymentMethodEnum.CC.ToString(),
                        OriginInbound = agreementInformation.OriginInbound
                    };

                    if (promotion != null)
                    {
                        zohoDto.PromoCodo = agreementInformation.Promocode;
                        if (promotion.ExtraCredits.HasValue && promotion.ExtraCredits.Value != 0)
                            zohoDto.DiscountType = ZohoDopplerValues.Credits;
                        else if (promotion.DiscountPercentage.HasValue && promotion.DiscountPercentage.Value != 0)
                            zohoDto.DiscountType = ZohoDopplerValues.Discount;
                    }

                    try
                    {
                        var contact = await _zohoService.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", string.Format("Email:equals:{0}", zohoDto.Email));
                        if (contact == null)
                        {
                            var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", string.Format("Email:equals:{0}", zohoDto.Email));
                            if (response != null)
                            {
                                var lead = response.Data.FirstOrDefault();
                                BillingHelper.MapForUpgrade(lead, zohoDto);
                                var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityLead> { Data = new List<ZohoEntityLead> { lead } }, settings);
                                await _zohoService.UpdateZohoEntityAsync(body, lead.Id, "Leads");
                            }
                        }
                        else
                        {
                            if (contact.AccountName != null && !string.IsNullOrEmpty(contact.AccountName.Name))
                            {
                                var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityAccount>>("Accounts", string.Format("Account_Name:equals:{0}", contact.AccountName.Name));
                                if (response != null)
                                {
                                    var account = response.Data.FirstOrDefault();
                                    BillingHelper.MapForUpgrade(account, zohoDto);
                                    var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityAccount> { Data = new List<ZohoEntityAccount> { account } }, settings);
                                    await _zohoService.UpdateZohoEntityAsync(body, account.Id, "Accounts");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var messageError = $"Failed at updating lead from zoho {accountname} with exception {e.Message}";
                        _logger.LogError(e, messageError);
                        await _slackService.SendNotification(messageError);
                    }
                }

                return new OkObjectResult("Successfully");
            }
            catch (Exception e)
            {
                var messageError = $"Failed at creating new agreement for user {accountname} with exception {e.Message}";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);
                return new ObjectResult("Failed at creating new agreement")
                {
                    StatusCode = 500
                };
            }
        }

        private async void SendNotifications(string accountname, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode, int discountId)
        {
            User userInformation = await _userRepository.GetUserInformation(accountname);

            bool isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion));

            if (newPlan.IdUserType == UserTypeEnum.INDIVIDUAL)
            {
                await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode, !isUpgradeApproved);
            }
            else
            {
                if (isUpgradeApproved && newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                {
                    await _emailTemplatesService.SendNotificationForSuscribersPlan(accountname, userInformation, newPlan);
                }

                var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(discountId);
                await _emailTemplatesService.SendNotificationForUpgradePlan(accountname, userInformation, newPlan, user, promotion, promocode, discountId, planDiscountInformation, !isUpgradeApproved);
            }
        }
    }
}

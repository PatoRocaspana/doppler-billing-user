using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class CreditCard
    {
        public string Number { get; set; }

        public string Code { get; set; }

        public int ExpirationMonth { get; set; }

        public int ExpirationYear { get; set; }

        public int ExpirationYearFull { get; set; }

        public string HolderName { get; set; }

        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string CountryCode { get; set; }

        public string Phonenumber { get; set; }

        public string PhoneType { get; set; }

        public int IdCountry { get; set; }

        public string IdentificationType { get; set; }

        public string IdentificationNumber { get; set; }

        public string Email { get; set; }

        public CardTypeEnum CardType { get; set; }
    }
}

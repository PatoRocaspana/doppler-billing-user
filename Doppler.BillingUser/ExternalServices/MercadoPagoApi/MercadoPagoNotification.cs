using System;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public class MercadoPagoNotification
    {
        public long Id { get; set; }
        public string Action { get; set; }
        public string ApiVersion { get; set; }
        public Data Data { get; set; }
        public DateTime DateCreated { get; set; }
        public bool LiveMode { get; set; }
        public string Type { get; set; }
        public long UserId { get; set; }
    }

    public class Data
    {
        public long Id { get; set; }
    }
}

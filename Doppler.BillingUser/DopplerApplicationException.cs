using Doppler.BillingUser.Enums;
using System;

namespace Doppler.BillingUser
{
    public class DopplerApplicationException : ApplicationException
    {
        public PaymentErrorCode ErrorCode { get; private set; }

        public string PaymentErrorKey { get; private set; }

        /// <summary>
        /// Creates an Application Error Code Exception.
        /// </summary>
        /// <param name="errorCode">The error code, cannot be null.</param>
        /// <param name="message">An optional error message.</param>
        /// <param name="innerException">Inner exception to be added.</param>
        public DopplerApplicationException(PaymentErrorCode errorCode, string message = null, Exception innerException = null)
            : base(errorCode + (message != null ? " - " + message : ""), innerException)
        {
            ErrorCode = errorCode;
            PaymentErrorKey = message;
        }
    }
}

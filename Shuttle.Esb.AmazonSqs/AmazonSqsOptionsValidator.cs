using System;
using Microsoft.Extensions.Options;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsOptionsValidator : IValidateOptions<AmazonSqsOptions>
    {
        public ValidateOptionsResult Validate(string name, AmazonSqsOptions options)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ValidateOptionsResult.Fail(Resources.AmazonSqsSettingsNameException);
            }

            if (string.IsNullOrWhiteSpace(options.ServiceUrl) ||
                !Uri.IsWellFormedUriString(options.ServiceUrl, UriKind.RelativeOrAbsolute))
            {
                return ValidateOptionsResult.Fail(string.Format(Resources.AmazonSqsSettingsServiceUrlException, name, options.ServiceUrl));
            }

            return ValidateOptionsResult.Success;
        }
    }
}
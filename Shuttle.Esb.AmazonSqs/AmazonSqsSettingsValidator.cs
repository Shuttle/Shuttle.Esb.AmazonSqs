using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsSettingsValidator : IValidateOptions<AmazonSqsSettings>
    {
        public ValidateOptionsResult Validate(string name, AmazonSqsSettings options)
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
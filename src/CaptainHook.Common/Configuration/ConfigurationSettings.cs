using System.Collections.Generic;

namespace CaptainHook.Common.Configuration
{
    public class ConfigurationSettings
    {
        public const string KeyVaultUriEnvVariable = "KEYVAULT_BASE_URI";

        public string AzureSubscriptionId { get; set; }

        public string InstrumentationKey { get; set; }

        public string ServiceBusConnectionString { get; set; }

        public string ServiceBusNamespace { get; set; }

        public string ApiName { get; set; }

        public string ApiSecret { get; set; }

        public List<string> RequiredScopes { get; set; }

        public string Authority { get; set; }

        public bool IsHttps => !string.IsNullOrWhiteSpace(Authority) && Authority.StartsWith("https");
    }
}

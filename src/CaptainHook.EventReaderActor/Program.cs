namespace CaptainHook.EventReaderActor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Integration.ServiceFabric;
    using Common;
    using Eshopworld.Core;
    using Eshopworld.Telemetry;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static async Task Main()
        {
            try
            {
                var config = new ConfigurationBuilder().AddAzureKeyVault(
                                                                  "https://esw-captain-hook-ci.vault.azure.net/", // DO THIS ENVIRONMENT BASED
                                                                  new KeyVaultClient(
                                                                      new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                                                                  new DefaultKeyVaultSecretManager())
                                                              .Build();

                var settings = new ConfigurationSettings();
                config.Bind(settings);

                var bb = new BigBrother("", "");
                bb.UseEventSourceSink().ForExceptions();

                var builder = new ContainerBuilder();
                builder.RegisterServiceFabricSupport();

                builder.RegisterActor<EventReaderActor>();
                builder.RegisterInstance(bb)
                       .As<IBigBrother>()
                       .SingleInstance();

                using (builder.Build())
                {
                    await Task.Delay(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                BigBrother.Write(e);
                throw;
            }
        }
    }
}

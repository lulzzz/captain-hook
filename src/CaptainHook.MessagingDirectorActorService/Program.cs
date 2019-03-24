using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.ServiceFabric;
using Eshopworld.Core;
using Eshopworld.Telemetry;

namespace CaptainHook.MessagingDirectorActorService
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static async Task Main()
        {
            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterServiceFabricSupport();
                builder.RegisterActor<MessagingDirectorActor>();
                builder.RegisterType<BigBrother>().As<IBigBrother>();

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

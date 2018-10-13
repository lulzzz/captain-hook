namespace CaptainHook.EventReaderActor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Integration.ServiceFabric;
    using Eshopworld.Core;
    using Eshopworld.Telemetry;

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static async Task Main()
        {
            try
            {
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

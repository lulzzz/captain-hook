using System;
using System.Threading;
using System.Threading.Tasks;
using Eshopworld.Telemetry;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.MessagingDirector
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
                ActorRuntime.RegisterActorAsync<MessagingDirector>((context, actorType) => new MessagingDirectorService(context, actorType, (service, id) => new MessagingDirector(service, id)))
                            .GetAwaiter()
                            .GetResult();

                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception e)
            {
                BigBrother.Write(e);
                throw;
            }
        }
    }
}

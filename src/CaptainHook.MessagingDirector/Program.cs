namespace CaptainHook.MessagingDirector
{
    using Microsoft.ServiceFabric.Actors.Runtime;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static async Task Main()
        {
            ActorRuntime.RegisterActorAsync<MessagingDirector>(
                            (context, actorType) =>
                                new MessagingDirectorService(context, actorType, (service, id) => new MessagingDirector(service, id)))
                        .GetAwaiter()
                        .GetResult();

            await Task.Delay(Timeout.Infinite);
        }
    }
}

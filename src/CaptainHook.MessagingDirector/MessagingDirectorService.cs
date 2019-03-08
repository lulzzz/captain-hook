using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Interfaces;
using Eshopworld.Telemetry;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.MessagingDirector
{
    internal class MessagingDirectorService : ActorService
    {
        public MessagingDirectorService(StatefulServiceContext context, ActorTypeInformation typeInfo, Func<ActorService, ActorId, ActorBase> newActor)
            : base(context, typeInfo, newActor)
        { }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await base.RunAsync(cancellationToken);

                var proxy = ActorProxy.Create<IMessagingDirector>(new ActorId(0));
                await proxy.Run();
            }
            catch (Exception e)
            {
                BigBrother.Write(e);
                throw;
            }
        }
    }
}

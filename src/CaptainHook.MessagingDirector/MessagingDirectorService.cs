namespace CaptainHook.MessagingDirector
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;

    internal class MessagingDirectorService : ActorService
    {
        public MessagingDirectorService(StatefulServiceContext context, ActorTypeInformation typeInfo, Func<ActorService, ActorId, ActorBase> newActor)
            : base(context, typeInfo, newActor)
        { }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);

            var proxy = ActorProxy.Create<IMessagingDirector>(new ActorId(0));
            await proxy.StartWork();
        }
    }
}
﻿using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.MessagingDirectorActor
{
    internal class MessagingDirectorActorService : ActorService
    {
        public MessagingDirectorActorService(StatefulServiceContext context, ActorTypeInformation typeInfo, Func<ActorService, ActorId, ActorBase> newActor)
            : base(context, typeInfo, newActor)
        { }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);

            var proxy = ActorProxy.Create<IMessagingDirector>(new ActorId(0));
            await proxy.Run();
        }
    }
}
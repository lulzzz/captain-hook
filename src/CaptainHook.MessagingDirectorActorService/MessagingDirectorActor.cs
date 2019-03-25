using System;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry;
using CaptainHook.Common.Telemetry.Actor;
using CaptainHook.Interfaces;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace CaptainHook.MessagingDirectorActorService
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    public class MessagingDirectorActor : Actor, IMessagingDirector
    {
        private readonly IBigBrother _bigBrother;
        private readonly ServiceBusConfig _serviceBusConfig;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of <see cref="T:CaptainHook.MessagingDirectorActorService.MessagingDirectorActor" />.
        /// </summary>
        /// <param name="actorService">The <see cref="T:Microsoft.ServiceFabric.Actors.Runtime.ActorService" /> that will host this actor instance.</param>
        /// <param name="actorId">The <see cref="T:Microsoft.ServiceFabric.Actors.ActorId" /> for this actor instance.</param>
        /// <param name="bigBrother"></param>
        /// <param name="serviceBusConfig"></param>
        public MessagingDirectorActor(
            ActorService actorService,
            ActorId actorId,
            IBigBrother bigBrother,
            ServiceBusConfig serviceBusConfig)
            : base(actorService, actorId)
        {
            this._bigBrother = bigBrother;
            _serviceBusConfig = serviceBusConfig;
        }

        /// <summary>
        /// Generates the Event Reader Name from the Actor
        /// </summary>
        public string EventReaderServiceName => $"{ApplicationName}/CaptainHook.EventReaderService";

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Task OnActivateAsync()
        {
            _bigBrother.Publish(new ActorActivatedEvent(this));
            return base.OnActivateAsync();

            //todo check that every reader is running and if not start it.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Task OnDeactivateAsync()
        {
            _bigBrother.Publish(new ActorDeactivatedEvent(this));
            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task Run(CancellationToken cancellationToken)
        {
            foreach (var domainType in await this.StateManager.GetStateNamesAsync(cancellationToken))
            {
                var webhookConfig = await ReadWebhook(domainType, cancellationToken);
                var proxy = ServiceProxy.Create<IEventReaderService>(new Uri(EventReaderServiceName),
                    ServicePartitionKey.Singleton);
                proxy.Configure(_serviceBusConfig, webhookConfig);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<WebhookConfig> ReadWebhook(string name, CancellationToken cancellationToken)
        {
            var conditionalValue = await StateManager.TryGetStateAsync<WebhookConfig>(name, cancellationToken);
            return conditionalValue.Value;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="config"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<WebhookConfig> CreateWebhook(WebhookConfig config, CancellationToken cancellationToken)
        {
            try
            {
                var result = await StateManager.TryAddStateAsync(config.Type, config, cancellationToken);

                if (!result)
                {
                    //_bigBrother.Publish(new ActorStateEvent(config.Type, "Cannot create new Webhook. Webhook with the same name has already been created"));
                    throw new Exception("Cannot create new Webhook. Webhook with the same name has already been created");
                }

                //todo do I need perms to this this here. Under what process isolation does this run as. Whats the perf hit to create a new reader each time.
                using (var client = new FabricClient())
                {
                    await client.ServiceManager.CreateServiceAsync(GetServiceDefinition(config.Type), TimeSpan.FromSeconds(30), cancellationToken);
                }

                result = await StateManager.TryAddStateAsync(config.Type, config, cancellationToken);
                if (!result)
                {
                    throw new Exception($"Was not able to persist the state of a new webhook - {config.Type}");
                }
                
                //todo how to send config information to the reliable service at startup rather than after.
                var proxy = ServiceProxy.Create<IEventReaderService>(new Uri(EventReaderServiceName), ServicePartitionKey.Singleton);
                proxy.Configure(_serviceBusConfig, config);

                _bigBrother.Publish(new WebHookCreatedEvent(config.Type));
                return await StateManager.GetStateAsync<WebhookConfig>(config.Type, cancellationToken);
            }
            catch (Exception e)
            {
                BigBrother.Write(e.ToExceptionEvent());
                throw;
            }
        }

        /// <summary>
        /// Creates a Service Definition for the new event reader
        /// </summary>
        /// <param name="uniquePostfix"></param>
        /// <returns></returns>
        private ServiceDescription GetServiceDefinition(string uniquePostfix)
        {
            return new StatefulServiceDescription
            {
                ApplicationName = new Uri(ApplicationName),
                ServiceName = new Uri($"{EventReaderServiceName}-{uniquePostfix}"),
                ServiceTypeName = "EventReaderType",
                HasPersistedState = true,
                PartitionSchemeDescription = new UniformInt64RangePartitionSchemeDescription(),
                MinReplicaSetSize = 1,
                TargetReplicaSetSize = 1
            };
        }

        public Task<WebhookConfig> UpdateWebhook(WebhookConfig config, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteWebhook(string type, CancellationToken cancellationToken)
        {
            try
            {
                var result = await StateManager.TryGetStateAsync<WebhookConfig>(type, cancellationToken);

                if (!result.HasValue)
                {
                    throw new Exception("actor not found");
                }

                _bigBrother.Publish(new ActorDeletedEvent(type, "Deleting actor based on api request"));

                using (var client = new FabricClient())
                {
                    //todo configure this timespan better
                    await client.ServiceManager.DeleteServiceAsync(new DeleteServiceDescription(new Uri(EventReaderServiceName)), TimeSpan.FromMinutes(30), cancellationToken);
                }

                await StateManager.RemoveStateAsync(type, cancellationToken);

                throw new Exception("actor not found");
            }
            catch (Exception e)
            {
                BigBrother.Write(e.ToExceptionEvent());
                throw;
            }
        }
    }
}

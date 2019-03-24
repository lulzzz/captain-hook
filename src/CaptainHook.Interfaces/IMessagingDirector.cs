using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Common.Configuration;
using Microsoft.ServiceFabric.Actors;

namespace CaptainHook.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IMessagingDirector : IActor
    {
        /// <summary>
        /// Run the director
        /// </summary>
        /// <returns></returns>
        Task Run(CancellationToken cancellationToken);

        /// <summary>
        /// Read an existing webhook config
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<WebhookConfig> ReadWebhook(string name, CancellationToken cancellationToken);

        /// <summary>
        /// Create a new webhook
        /// </summary>
        /// <param name="config"></param>
        /// <param name="cancellationToken"></param>
        Task<WebhookConfig> CreateWebhook(WebhookConfig config, CancellationToken cancellationToken);

        /// <summary>
        /// Update an existing webhook
        /// //todo need to think about authorization
        /// </summary>
        /// <param name="config"></param>
        /// <param name="cancellationToken"></param>
        WebhookConfig UpdateWebhook(WebhookConfig config, CancellationToken cancellationToken);

        /// <summary>
        /// Delete an existing webhook
        /// /todo need to think about authorization
        /// </summary>
        /// <param name="name">Name given to the webhook</param>
        /// <param name="cancellationToken"></param>
        Task DeleteWebhook(string name, CancellationToken cancellationToken);
    }
}

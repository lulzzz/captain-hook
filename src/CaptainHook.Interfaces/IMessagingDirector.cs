using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Common.Configuration;
using Microsoft.ServiceFabric.Actors;

namespace CaptainHook.Interfaces
{
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
        Task Run();

        /// <summary>
        /// Read an existing webhook config
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<WebhookConfig> ReadWebhookAsync(string name);

        /// <summary>
        /// Create a new webhook
        /// </summary>
        /// <param name="config"></param>
        Task<WebhookConfig> CreateWebhookAsync(WebhookConfig config);

        /// <summary>
        /// Update an existing webhook
        /// //todo need to think about authorisation
        /// </summary>
        /// <param name="config"></param>
        WebhookConfig UpdateWebhook(WebhookConfig config);

        /// <summary>
        /// Delete an existing webhook
        /// /todo need to think about authorisation
        /// </summary>
        /// <param name="name">Name given to the webhook</param>
        /// <param name="cancellationToken"></param>
        Task DeleteWebhookAsync(string name, CancellationToken cancellationToken);
    }
}

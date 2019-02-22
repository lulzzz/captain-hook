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
        WebhookConfig ReadWebhook(string name);

        /// <summary>
        /// Create a new webhook
        /// </summary>
        /// <param name="config"></param>
        WebhookConfig CreateWebhook(WebhookConfig config);

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
        void DeleteWebhook(string name);
    }
}

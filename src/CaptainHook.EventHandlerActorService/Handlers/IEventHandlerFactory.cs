using CaptainHook.Common.Configuration;

namespace CaptainHook.EventHandlerActorService.Handlers
{
    public interface IEventHandlerFactory
    {
        /// <summary>
        /// Create the custom handler such that we get a mapping from the webhook to the handler selected
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="webhookConfig"></param>
        /// <returns></returns>
        IHandler CreateWebhookWithCallbackHandler(string eventType, WebhookConfig webhookConfig);

        /// <summary>
        /// Used only for getting the callback handler
        /// </summary>
        /// <param name="webhookConfig"></param>
        /// <returns></returns>
        IHandler CreateWebhookHandler(WebhookConfig webhookConfig);
    }
}

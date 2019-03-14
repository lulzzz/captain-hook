namespace CaptainHook.EventReaderService
{
    public interface IEventReader : IService
    {
        void Configure(TopicConfig topicConfig, WebhookConfig webhookConfig);

        Task CompleteMessage(Guid handle);
    }
}
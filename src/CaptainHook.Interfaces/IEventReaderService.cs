using System;
using System.Threading.Tasks;
using CaptainHook.Common.Configuration;
using Microsoft.ServiceFabric.Services.Remoting;

namespace CaptainHook.Interfaces
{
    public interface IEventReaderService : IService
    {
        void Configure(TopicConfig topicConfig, WebhookConfig webhookConfig);

        Task CompleteMessage(Guid handle);
    }
}
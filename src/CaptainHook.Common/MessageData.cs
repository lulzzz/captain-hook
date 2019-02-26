using System;
using CaptainHook.Common.Configuration;

namespace CaptainHook.Common
{
    public class MessageData
    {
        // ReSharper disable once UnusedMember.Local - Use by the data contract serializers
        private MessageData() { }

        public MessageData(string payload, string type)
        {
            Handle = Guid.NewGuid();
            Payload = payload;
            Type = type;
        }

        public Guid Handle { get; }

        public int HandlerId { get; set; }

        public string Payload { get; }

        public string Type { get; }

        public string EventHandlerActorId => $"{Type}-{HandlerId}";

        /// <summary>
        /// //todo trying something with the config to process the message is added to the message so any availabe handler and dispatcher can process it.
        /// </summary>
        public WebhookConfig WebhookConfig { get; set; }
    }

    public class MessageDataHandle
    {
        public Guid Handle { get; set; }

        public int HandlerId { get; set; }

        public string LockToken { get; set; }
    }
}

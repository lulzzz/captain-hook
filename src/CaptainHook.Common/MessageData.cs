namespace CaptainHook.Common
{
    using System;
    using JetBrains.Annotations;

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
    }

    public class MessageDataHandle
    {
        public Guid Handle { get; set; }

        public int HandlerId { get; set; }

        public string LockToken { get; set; }
    }
}

﻿namespace CaptainHook.Common
{
    using System;

    public class MessageData
    {
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
}

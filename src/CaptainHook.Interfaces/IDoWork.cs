namespace CaptainHook.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using Common;

    public interface IDoWork
    {
        Task<Guid> DoWork(MessageData messageData);
    }
}

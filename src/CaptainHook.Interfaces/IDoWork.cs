using System;
using System.Threading.Tasks;
using CaptainHook.Common;

namespace CaptainHook.Interfaces
{
    public interface IDoWork
    {
        Task<Guid> DoWork(MessageData messageData);
    }
}

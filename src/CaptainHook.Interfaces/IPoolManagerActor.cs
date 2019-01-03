using System;
using System.Threading.Tasks;
using CaptainHook.Common;
using Microsoft.ServiceFabric.Actors;

namespace CaptainHook.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IPoolManagerActor : IActor
    {
        Task<Guid> DoWork(MessageData messageData);

        Task CompleteWork(Guid handle);
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace CaptainHook.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IEventReaderActor : IActor
    {
        Task Run();

        Task CompleteMessage(Guid handle);

        Task FailMessage(Guid handle);
    }
}

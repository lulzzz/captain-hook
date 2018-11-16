namespace CaptainHook.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    public interface IDoCompletions : IActor
    {
        Task CompleteWork(Guid id);
    }
}

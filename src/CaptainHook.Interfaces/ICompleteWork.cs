namespace CaptainHook.Interfaces
{
    using System.Threading.Tasks;
    using Common;
    using Microsoft.ServiceFabric.Actors;

    public interface ICompleteWork : IActor
    {
        Task CompleteWork(MessageData messageData);

        Task FailWork(MessageData messageData);
    }
}

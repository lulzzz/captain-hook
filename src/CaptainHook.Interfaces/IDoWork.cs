namespace CaptainHook.Interfaces
{
    using System.Threading.Tasks;
    using Common;
    using Microsoft.ServiceFabric.Actors;

    public interface IDoWork : IActor
    {
        Task DoWork(MessageData messageData);
    }
}

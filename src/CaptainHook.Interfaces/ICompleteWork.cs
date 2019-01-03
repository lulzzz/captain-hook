using System.Threading.Tasks;
using CaptainHook.Common;

namespace CaptainHook.Interfaces
{
    public interface ICompleteWork
    {
        Task CompleteWork(MessageData messageData);

        Task FailWork(MessageData messageData);
    }
}
